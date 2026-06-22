using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.DTOs;
using Prode.Api.Entities;

namespace Prode.Api.Services;

public class MechanicsService
{
    private readonly AppDbContext _context;

    public MechanicsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MechanicsStateDto> GetStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var captain = await _context.CaptainPicks
            .AsNoTracking()
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        var now = DateTime.UtcNow;

        return new MechanicsStateDto
        {
            Captain = captain == null
                ? null
                : new CaptainPickDto
                {
                    TeamId = captain.TeamId,
                    TeamName = captain.Team.Name,
                    IsLocked = await IsCaptainWindowClosedAsync(now, cancellationToken)
                },
            GoldenGoals = await _context.GoldenGoalPicks
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new GoldenGoalPickDto
                {
                    RoundKey = p.RoundKey,
                    MatchId = p.MatchId
                })
                .ToListAsync(cancellationToken),
            SharpShooters = await _context.SharpShooterPredictions
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new SharpShooterPickDto
                {
                    RoundKey = p.RoundKey,
                    MatchId = p.MatchId,
                    PointsAwarded = p.PointsAwarded
                })
                .ToListAsync(cancellationToken),
            OraclePredictions = await _context.OraclePredictions
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new OraclePredictionDto
                {
                    RoundKey = p.RoundKey,
                    DrawsAfterNinetyPrediction = p.DrawsAfterNinetyPrediction,
                    PenaltyShootoutsPrediction = p.PenaltyShootoutsPrediction
                })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task SelectCaptainAsync(
        Guid userId,
        int teamId,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;

        if (await IsCaptainWindowClosedAsync(now, cancellationToken))
        {
            throw new InvalidOperationException("La eleccion de capitan ya esta cerrada");
        }

        var teamExists = await _context.Teams
            .AnyAsync(t => t.Id == teamId, cancellationToken);

        if (!teamExists)
        {
            throw new InvalidOperationException("Seleccion no encontrada");
        }

        var existing = await _context.CaptainPicks
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (existing == null)
        {
            _context.CaptainPicks.Add(new CaptainPick
            {
                UserId = userId,
                TeamId = teamId
            });
        }
        else
        {
            existing.TeamId = teamId;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SelectGoldenGoalAsync(
        Guid userId,
        int matchId,
        CancellationToken cancellationToken = default
    )
    {
        var match = await GetSelectableMatchAsync(matchId, cancellationToken);
        var roundKey = WorldCupRoundService.GetRoundKey(match);

        if (!WorldCupRoundService.IsKnockoutRound(roundKey))
        {
            throw new InvalidOperationException("Gol de Oro solo aplica a rondas eliminatorias");
        }

        if (match.PredictionsLocked || match.MatchDate <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("El Gol de Oro debe elegirse antes del inicio del partido");
        }

        var existing = await _context.GoldenGoalPicks
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.RoundKey == roundKey,
                cancellationToken
            );

        if (existing == null)
        {
            _context.GoldenGoalPicks.Add(new GoldenGoalPick
            {
                UserId = userId,
                MatchId = match.Id,
                RoundKey = roundKey
            });
        }
        else
        {
            existing.MatchId = match.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SelectSharpShooterAsync(
        Guid userId,
        int matchId,
        CancellationToken cancellationToken = default
    )
    {
        var match = await GetSelectableMatchAsync(matchId, cancellationToken);
        var roundKey = WorldCupRoundService.GetRoundKey(match);

        if (!WorldCupRoundService.IsKnockoutRound(roundKey))
        {
            throw new InvalidOperationException("Francotirador solo aplica a rondas eliminatorias");
        }

        await EnsureRoundHasNotStartedAsync(roundKey, cancellationToken);

        var existing = await _context.SharpShooterPredictions
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.RoundKey == roundKey,
                cancellationToken
            );

        if (existing == null)
        {
            _context.SharpShooterPredictions.Add(new SharpShooterPrediction
            {
                UserId = userId,
                MatchId = match.Id,
                RoundKey = roundKey
            });
        }
        else
        {
            existing.MatchId = match.Id;
            existing.PointsAwarded = 0;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitOraclePredictionAsync(
        Guid userId,
        SubmitOraclePredictionDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var roundKey = dto.RoundKey.Trim().ToUpperInvariant();

        if (!WorldCupRoundService.IsKnockoutRound(roundKey))
        {
            throw new InvalidOperationException("Ronda eliminatoria invalida");
        }

        if (dto.DrawsAfterNinetyPrediction < 0 || dto.PenaltyShootoutsPrediction < 0)
        {
            throw new InvalidOperationException("Las predicciones no pueden ser negativas");
        }

        var roundMatches = await GetRoundMatchesAsync(roundKey, cancellationToken);

        if (dto.DrawsAfterNinetyPrediction > roundMatches.Count ||
            dto.PenaltyShootoutsPrediction > roundMatches.Count)
        {
            throw new InvalidOperationException("La prediccion excede la cantidad de partidos de la ronda");
        }

        await EnsureRoundHasNotStartedAsync(roundKey, cancellationToken);

        var existing = await _context.OraclePredictions
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.RoundKey == roundKey,
                cancellationToken
            );

        if (existing == null)
        {
            _context.OraclePredictions.Add(new OraclePrediction
            {
                UserId = userId,
                RoundKey = roundKey,
                DrawsAfterNinetyPrediction = dto.DrawsAfterNinetyPrediction,
                PenaltyShootoutsPrediction = dto.PenaltyShootoutsPrediction
            });
        }
        else
        {
            existing.DrawsAfterNinetyPrediction = dto.DrawsAfterNinetyPrediction;
            existing.PenaltyShootoutsPrediction = dto.PenaltyShootoutsPrediction;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Match> GetSelectableMatchAsync(
        int matchId,
        CancellationToken cancellationToken
    )
    {
        var match = await _context.Matches
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        if (match == null)
        {
            throw new InvalidOperationException("Partido no encontrado");
        }

        if (!match.HomeTeamId.HasValue || !match.AwayTeamId.HasValue)
        {
            throw new InvalidOperationException("Partido pendiente de definicion");
        }

        return match;
    }

    private async Task EnsureRoundHasNotStartedAsync(
        string roundKey,
        CancellationToken cancellationToken
    )
    {
        var roundMatches = await GetRoundMatchesAsync(roundKey, cancellationToken);

        if (roundMatches.Count == 0)
        {
            throw new InvalidOperationException("La ronda no tiene partidos cargados");
        }

        if (WorldCupRoundService.IsRoundStartLocked(roundMatches, DateTime.UtcNow))
        {
            throw new InvalidOperationException("La ronda ya comenzo");
        }
    }

    private async Task<bool> IsCaptainWindowClosedAsync(
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var matches = await GetRoundMatchesAsync(
            WorldCupRoundService.RoundOf32,
            cancellationToken
        );

        var firstRoundOf32 = matches.OrderBy(m => m.MatchDate).FirstOrDefault();

        return firstRoundOf32 != null &&
            (firstRoundOf32.PredictionsLocked || firstRoundOf32.MatchDate <= now);
    }

    private async Task<List<Match>> GetRoundMatchesAsync(
        string roundKey,
        CancellationToken cancellationToken
    )
    {
        var matches = await _context.Matches
            .AsNoTracking()
            .Where(m => !string.IsNullOrWhiteSpace(m.Stage))
            .ToListAsync(cancellationToken);

        return matches
            .Where(m => WorldCupRoundService.GetRoundKey(m) == roundKey)
            .OrderBy(m => m.MatchDate)
            .ToList();
    }
}
