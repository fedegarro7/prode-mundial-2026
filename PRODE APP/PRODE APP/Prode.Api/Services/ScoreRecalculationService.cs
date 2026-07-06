using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.Entities;

namespace Prode.Api.Services;

public class ScoreRecalculationService
{
    public const string OracleDrawsAward = "ORACLE_DRAWS";
    public const string OraclePenaltiesAward = "ORACLE_PENALTIES";
    public const string RoundKingAward = "ROUND_KING";

    private readonly AppDbContext _context;
    private readonly ScoringService _scoringService;
    private readonly BombMatchService _bombMatchService;

    public ScoreRecalculationService(
        AppDbContext context,
        ScoringService scoringService,
        BombMatchService bombMatchService
    )
    {
        _context = context;
        _scoringService = scoringService;
        _bombMatchService = bombMatchService;
    }

    public async Task RecalculateForMatchAsync(
        Match match,
        CancellationToken cancellationToken = default
    )
    {
        var roundKey = WorldCupRoundService.GetRoundKey(match);

        if (WorldCupRoundService.IsKnockoutRound(roundKey))
        {
            await RecalculateForRoundAsync(roundKey, cancellationToken);
            return;
        }

        var trackedMatch = await _context.Matches
            .Include(m => m.Predictions)
            .FirstAsync(m => m.Id == match.Id, cancellationToken);

        await ScoreMatchPredictionsAsync(trackedMatch, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecalculateForRoundAsync(
        string roundKey,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;
        var bombMatch = await _bombMatchService.EnsureAssignedForRoundAsync(
            roundKey,
            now,
            cancellationToken
        );

        var roundMatches = await _context.Matches
            .Include(m => m.Predictions)
            .Where(m => !string.IsNullOrWhiteSpace(m.Stage))
            .ToListAsync(cancellationToken);

        roundMatches = roundMatches
            .Where(m => WorldCupRoundService.GetRoundKey(m) == roundKey)
            .OrderBy(m => m.MatchDate)
            .ToList();

        foreach (var roundMatch in roundMatches)
        {
            await ScoreMatchPredictionsAsync(roundMatch, bombMatch, cancellationToken);
        }

        await RecalculateSharpShooterAsync(roundKey, cancellationToken);
        await RecalculateRoundAwardsAsync(roundKey, roundMatches, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ScoreMatchPredictionsAsync(
        Match match,
        BombMatch? bombMatch,
        CancellationToken cancellationToken
    )
    {
        if (match.Predictions.Count == 0)
        {
            return;
        }

        var userIds = match.Predictions.Select(p => p.UserId).Distinct().ToList();
        var roundKey = WorldCupRoundService.GetRoundKey(match);
        var isKnockout = WorldCupRoundService.IsKnockoutRound(roundKey);

        var goldenGoalUserIds = isKnockout
            ? await _context.GoldenGoalPicks
                .Where(p =>
                    p.MatchId == match.Id &&
                    p.RoundKey == roundKey &&
                    userIds.Contains(p.UserId)
                )
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken)
            : [];

        // Captain bonus only applies from the knockout phase onwards.
        var captainTeamsByUser = isKnockout
            ? await _context.CaptainPicks
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.TeamId, cancellationToken)
            : new Dictionary<Guid, int>();

        var goldenGoalSet = goldenGoalUserIds.ToHashSet();
        var isBombMatch = bombMatch?.MatchId == match.Id;

        foreach (var prediction in match.Predictions)
        {
            captainTeamsByUser.TryGetValue(prediction.UserId, out var captainTeamId);
            var hasCaptainTeam = captainTeamId != 0 &&
                (match.HomeTeamId == captainTeamId || match.AwayTeamId == captainTeamId);

            var score = _scoringService.CalculatePredictionScore(
                prediction,
                match,
                new ScoringContext(
                    goldenGoalSet.Contains(prediction.UserId),
                    isBombMatch,
                    hasCaptainTeam
                )
            );

            prediction.BasePointsEarned = score.BasePoints;
            prediction.MultiplierBonusPoints = score.MultiplierBonusPoints;
            prediction.CaptainBonusPoints = score.CaptainBonusPoints;
            prediction.PointsEarned = score.TotalPoints;
        }
    }

    private async Task RecalculateSharpShooterAsync(
        string roundKey,
        CancellationToken cancellationToken
    )
    {
        var picks = await _context.SharpShooterPredictions
            .Include(p => p.Match)
            .Where(p => p.RoundKey == roundKey)
            .ToListAsync(cancellationToken);

        foreach (var pick in picks)
        {
            pick.PointsAwarded =
                pick.Match.IsFinished && pick.Match.WasDecidedByPenalties
                    ? 5
                    : 0;
        }
    }

    private async Task RecalculateRoundAwardsAsync(
        string roundKey,
        List<Match> roundMatches,
        CancellationToken cancellationToken
    )
    {
        var existingAwards = await _context.RoundAwards
            .Where(a =>
                a.RoundKey == roundKey &&
                (a.AwardType == OracleDrawsAward ||
                 a.AwardType == OraclePenaltiesAward ||
                 a.AwardType == RoundKingAward)
            )
            .ToListAsync(cancellationToken);

        _context.RoundAwards.RemoveRange(existingAwards);

        if (roundMatches.Count == 0 || roundMatches.Any(m => !m.IsFinished))
        {
            return;
        }

        AwardOraclePoints(roundKey, roundMatches);
        await AwardRoundKingPointsAsync(roundKey, roundMatches, cancellationToken);
    }

    private void AwardOraclePoints(string roundKey, List<Match> roundMatches)
    {
        // A "draw after 90 min" includes matches that went to penalties (scores stay tied)
        // AND matches decided in extra time (WentToExtraTime = true, final score differs).
        var realDraws = roundMatches.Count(m =>
            (m.HomeScore == m.AwayScore) || m.WentToExtraTime);
        var realPenalties = roundMatches.Count(m => m.WasDecidedByPenalties);

        var predictions = _context.OraclePredictions
            .Where(p => p.RoundKey == roundKey)
            .AsEnumerable()
            .ToList();

        AwardClosestOracleCategory(
            predictions,
            roundKey,
            OracleDrawsAward,
            realDraws,
            p => p.DrawsAfterNinetyPrediction
        );

        AwardClosestOracleCategory(
            predictions,
            roundKey,
            OraclePenaltiesAward,
            realPenalties,
            p => p.PenaltyShootoutsPrediction
        );
    }

    private void AwardClosestOracleCategory(
        List<OraclePrediction> predictions,
        string roundKey,
        string awardType,
        int realValue,
        Func<OraclePrediction, int> selector
    )
    {
        if (predictions.Count == 0)
        {
            return;
        }

        var bestDistance = predictions.Min(p => Math.Abs(selector(p) - realValue));
        var winners = predictions
            .Where(p => Math.Abs(selector(p) - realValue) == bestDistance)
            .Select(p => p.UserId)
            .Distinct();

        foreach (var userId in winners)
        {
            _context.RoundAwards.Add(new RoundAward
            {
                UserId = userId,
                RoundKey = roundKey,
                AwardType = awardType,
                PointsAwarded = 5
            });
        }
    }

    private async Task AwardRoundKingPointsAsync(
        string roundKey,
        List<Match> roundMatches,
        CancellationToken cancellationToken
    )
    {
        var matchIds = roundMatches.Select(m => m.Id).ToHashSet();
        var userScores = roundMatches
            .SelectMany(m => m.Predictions)
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.PointsEarned));

        var sharpShooterScores = await _context.SharpShooterPredictions
            .Where(p => p.RoundKey == roundKey)
            .GroupBy(p => p.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Points = g.Sum(p => p.PointsAwarded)
            })
            .ToListAsync(cancellationToken);

        foreach (var score in sharpShooterScores)
        {
            userScores[score.UserId] =
                userScores.GetValueOrDefault(score.UserId) + score.Points;
        }

        if (userScores.Count == 0)
        {
            return;
        }

        var topScore = userScores.Values.Max();

        if (topScore <= 0)
        {
            return;
        }

        foreach (var userId in userScores.Where(s => s.Value == topScore).Select(s => s.Key))
        {
            _context.RoundAwards.Add(new RoundAward
            {
                UserId = userId,
                RoundKey = roundKey,
                AwardType = RoundKingAward,
                PointsAwarded = 3
            });
        }
    }
}
