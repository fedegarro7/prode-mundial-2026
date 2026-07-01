using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Prode.Api.Services;
using Prode.Api.DTOs;
using System.Security.Claims;
using Prode.Api.Entities;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _context;

    private readonly ScoreRecalculationService _scoreRecalculationService;

    private readonly BombMatchService _bombMatchService;

    public MatchesController(
        AppDbContext context,
        ScoreRecalculationService scoreRecalculationService,
        BombMatchService bombMatchService
    )
    {
        _context = context;
        _scoreRecalculationService = scoreRecalculationService;
        _bombMatchService = bombMatchService;
    }

    [HttpPost("recalculate-round/{roundKey}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecalculateRound(string roundKey)
    {
        await _scoreRecalculationService.RecalculateForRoundAsync(roundKey);
        return Ok(new { message = $"Recalculación completada para ronda '{roundKey}'." });
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var matches = await _context.Matches
            .AsNoTracking()
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .Include(x => x.Stadium)
            .OrderBy(x => x.MatchDate)
            .ToListAsync();

        var response = matches
            .Select(match => ToMatchDetailsDto(match, null))
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
    CreateMatchDto dto
)
    {
        if (
            dto.HomeTeamId.HasValue &&
            dto.AwayTeamId.HasValue &&
            dto.HomeTeamId == dto.AwayTeamId
        )
        {
            return BadRequest(
                "El equipo local y visitante deben ser distintos"
            );
        }

        if (
            !dto.HomeTeamId.HasValue &&
            string.IsNullOrWhiteSpace(dto.HomePlaceholder)
        )
        {
            return BadRequest("Falta equipo local o placeholder");
        }

        if (
            !dto.AwayTeamId.HasValue &&
            string.IsNullOrWhiteSpace(dto.AwayPlaceholder)
        )
        {
            return BadRequest("Falta equipo visitante o placeholder");
        }

        var teamIds = new[]
            {
                dto.HomeTeamId,
                dto.AwayTeamId
            }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var teamsCount = await _context.Teams
            .CountAsync(x => teamIds.Contains(x.Id));

        if (teamsCount != teamIds.Count)
        {
            return BadRequest("Equipo local o visitante invalido");
        }

        var stadiumExists = await _context.Stadiums
            .AnyAsync(x => x.Id == dto.StadiumId);

        if (!stadiumExists)
        {
            return BadRequest("Estadio invalido");
        }

        var match = new Match
        {
            MatchNumber = dto.MatchNumber,
            HomeTeamId = dto.HomeTeamId,
            HomePlaceholder = dto.HomePlaceholder,
            AwayTeamId = dto.AwayTeamId,
            AwayPlaceholder = dto.AwayPlaceholder,
            MatchDate = NormalizeUtc(dto.MatchDate),
            Stage = dto.Stage,
            GroupName = dto.GroupName,
            StadiumId = dto.StadiumId,
            HomeScore = dto.HomeScore,
            AwayScore = dto.AwayScore,
            IsFinished = dto.IsFinished,
            PredictionsLocked =
                dto.PredictionsLocked ||
                !dto.HomeTeamId.HasValue ||
                !dto.AwayTeamId.HasValue
        };

        _context.Matches.Add(match);

        await _context.SaveChangesAsync();

        var createdMatch = await _context.Matches
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .Include(x => x.Stadium)
            .FirstAsync(x => x.Id == match.Id);

        return Ok(ToMatchDetailsDto(createdMatch, null, false));
    }
    [HttpPut("{id}/result")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetResult(
     int id,
     [FromBody] SetMatchResultDto result
 )
    {
        var match = await _context.Matches
            .Include(x => x.Predictions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (match == null)
        {
            return NotFound("Partido no encontrado");
        }

        match.HomeScore = result.HomeScore;

        match.AwayScore = result.AwayScore;

        match.WasDecidedByPenalties = result.WasDecidedByPenalties;

        match.IsFinished = true;

        match.PredictionsLocked = true;

        await _scoreRecalculationService.RecalculateForMatchAsync(match);

        return Ok(new
        {
            message = "Resultado cargado correctamente"
        });
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        Guid? userId = null;

        var userIdClaim = User.Claims.FirstOrDefault(
            x => x.Type == ClaimTypes.NameIdentifier
        );

        if (userIdClaim != null)
        {
            userId = Guid.Parse(userIdClaim.Value);
        }

        var match = await _context.Matches
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .Include(x => x.Stadium)
            .Include(x => x.Predictions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (match == null)
        {
            return NotFound("Partido no encontrado");
        }

        Prediction? myPrediction = null;

        if (userId.HasValue)
        {
            myPrediction = match.Predictions
                .FirstOrDefault(x => x.UserId == userId.Value);
        }

        var visibleBombMatches = await _bombMatchService.GetVisibleBombMatchesAsync(DateTime.UtcNow);
        var response = ToMatchDetailsDto(match, myPrediction, visibleBombMatches.ContainsKey(match.Id));

        return Ok(response);
    }

    [HttpGet("upcoming")]
    [Authorize]
    public async Task<IActionResult> Upcoming()
    {
        var userIdClaim = User.Claims.FirstOrDefault(
            x => x.Type == ClaimTypes.NameIdentifier
        );

        if (userIdClaim == null)
        {
            return Unauthorized();
        }

        var userId = Guid.Parse(userIdClaim.Value);

        var matches = await _context.Matches
            .Include(x => x.Predictions)
                .ThenInclude(x => x.User)
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .Include(x => x.Stadium)
            .Where(x => x.IsFinished || x.MatchDate > DateTime.UtcNow.AddHours(-3))
            .OrderBy(x => x.MatchDate)
            .ToListAsync();

        var userGroups = await _context.PrivateGroups
            .AsNoTracking()
            .Include(g => g.Owner)
            .Include(g => g.Memberships.Where(m => m.Status == MembershipStatus.Approved))
                .ThenInclude(m => m.User)
            .Where(g =>
                g.OwnerId == userId ||
                g.Memberships.Any(m =>
                    m.UserId == userId &&
                    m.Status == MembershipStatus.Approved
                )
            )
            .OrderBy(g => g.Name)
            .ToListAsync();

        var visibleBombMatches = await _bombMatchService.GetVisibleBombMatchesAsync(DateTime.UtcNow);

        var allGroupUserIds = userGroups
            .SelectMany(g => g.Memberships.Select(m => m.UserId).Append(g.OwnerId))
            .Append(userId)
            .Distinct()
            .ToList();

        var captainPicks = allGroupUserIds.Count > 0
            ? await _context.CaptainPicks
                .AsNoTracking()
                .Where(p => allGroupUserIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.TeamId)
            : new Dictionary<Guid, int>();

        var goldenGoalSet = allGroupUserIds.Count > 0
            ? (await _context.GoldenGoalPicks
                .AsNoTracking()
                .Where(p => allGroupUserIds.Contains(p.UserId))
                .Select(p => new { p.UserId, p.MatchId })
                .ToListAsync())
                .Select(p => (p.UserId, p.MatchId))
                .ToHashSet()
            : new HashSet<(Guid, int)>();

        var sharpShooterDict = allGroupUserIds.Count > 0
            ? (await _context.SharpShooterPredictions
                .AsNoTracking()
                .Where(p => allGroupUserIds.Contains(p.UserId))
                .Select(p => new { p.UserId, p.MatchId, p.PointsAwarded })
                .ToListAsync())
                .ToDictionary(p => (p.UserId, p.MatchId), p => p.PointsAwarded)
            : new Dictionary<(Guid, int), int>();

        var response = matches
            .Select(match => new UpcomingMatchDto
            {
                Id = match.Id,

                FifaId = match.FifaId ?? string.Empty,

                MatchNumber = match.MatchNumber,

                HomeTeam = ToTeamDto(match.HomeTeam),

                HomePlaceholder = match.HomePlaceholder,

                AwayTeam = ToTeamDto(match.AwayTeam),

                AwayPlaceholder = match.AwayPlaceholder,

                MatchDate = match.MatchDate,

                Stage = match.Stage,

                GroupName = match.GroupName,

                Stadium = ToStadiumDto(match.Stadium),

                PredictionsLocked =
                    match.PredictionsLocked,

                HomeScore = match.HomeScore,

                AwayScore = match.AwayScore,

                IsFinished = match.IsFinished,

                MyPrediction =
                    match.Predictions
                        .Where(p => p.UserId == userId)
                        .Select(p => new MyPredictionDto
                        {
                            HomeScorePrediction =
                                p.HomeScorePrediction,

                            AwayScorePrediction =
                                p.AwayScorePrediction,

                            PointsEarned = match.IsFinished
                                ? p.PointsEarned +
                                  (sharpShooterDict.TryGetValue((userId, match.Id), out var mySSBonus)
                                      ? mySSBonus : 0)
                                : 0,

                            BasePointsEarned =
                                match.IsFinished ? p.BasePointsEarned : 0,

                            MultiplierBonusPoints =
                                match.IsFinished ? p.MultiplierBonusPoints : 0,

                            CaptainBonusPoints =
                                match.IsFinished ? p.CaptainBonusPoints : 0
                        })
                        .FirstOrDefault(),

                GroupPredictions =
                    BuildGroupPredictions(match, userGroups, userId, captainPicks, goldenGoalSet, sharpShooterDict),

                IsBombMatch = visibleBombMatches.ContainsKey(match.Id)
            })
            .ToList();

        return Ok(response);
    }

    [HttpGet("finished")]
    [AllowAnonymous]
    public async Task<IActionResult> Finished()
    {
        var matches = await _context.Matches
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .Include(x => x.Stadium)
            .Where(x => x.IsFinished)
            .OrderByDescending(x => x.MatchDate)
            .ToListAsync();

        var response = matches
            .Select(match => new
            {
                match.Id,

                match.FifaId,

                match.MatchNumber,

                HomeTeam = ToTeamDto(match.HomeTeam),

                match.HomePlaceholder,

                AwayTeam = ToTeamDto(match.AwayTeam),

                match.AwayPlaceholder,

                match.MatchDate,

                match.Stage,

                match.GroupName,

                Stadium = ToStadiumDto(match.Stadium),

                match.HomeScore,

                match.AwayScore
            })
            .ToList();

        return Ok(response);
    }

    private static MatchDetailsDto ToMatchDetailsDto(
        Match match,
        Prediction? myPrediction,
        bool isBombMatch = false
    )
    {
        return new MatchDetailsDto
        {
            Id = match.Id,
            FifaId = match.FifaId ?? string.Empty,
            MatchNumber = match.MatchNumber,
            HomeTeam = ToTeamDto(match.HomeTeam),
            HomePlaceholder = match.HomePlaceholder,
            AwayTeam = ToTeamDto(match.AwayTeam),
            AwayPlaceholder = match.AwayPlaceholder,
            MatchDate = match.MatchDate,
            Stage = match.Stage,
            GroupName = match.GroupName,
            Stadium = ToStadiumDto(match.Stadium),
            HomeScore = match.HomeScore,
            AwayScore = match.AwayScore,
            IsFinished = match.IsFinished,
            WasDecidedByPenalties = match.WasDecidedByPenalties,
            PredictionsLocked = match.PredictionsLocked,
            IsBombMatch = isBombMatch,

            MyPrediction = myPrediction == null
                ? null
                : new MyPredictionDto
                {
                    HomeScorePrediction =
                        myPrediction.HomeScorePrediction,

                    AwayScorePrediction =
                        myPrediction.AwayScorePrediction,

                    PointsEarned =
                        match.IsFinished ? myPrediction.PointsEarned : 0,

                    BasePointsEarned =
                        match.IsFinished ? myPrediction.BasePointsEarned : 0,

                    MultiplierBonusPoints =
                        match.IsFinished ? myPrediction.MultiplierBonusPoints : 0,

                    CaptainBonusPoints =
                        match.IsFinished ? myPrediction.CaptainBonusPoints : 0
                }
        };
    }

    private static List<MatchGroupPredictionsDto> BuildGroupPredictions(
        Match match,
        List<PrivateGroup> userGroups,
        Guid currentUserId,
        Dictionary<Guid, int> captainPicks,
        HashSet<(Guid UserId, int MatchId)> goldenGoalSet,
        Dictionary<(Guid UserId, int MatchId), int> sharpShooterDict
    )
    {
        var canRevealPredictions =
            match.IsFinished ||
            match.PredictionsLocked ||
            match.MatchDate <= DateTime.UtcNow;

        if (!canRevealPredictions)
        {
            return [];
        }

        var isKnockout = string.IsNullOrWhiteSpace(match.GroupName);

        var predictionsByUser = match.Predictions
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Id).First());

        return userGroups
            .Select(group =>
            {
                var members = group.Memberships
                    .Where(m => m.Status == MembershipStatus.Approved)
                    .Select(m => m.User)
                    .Append(group.Owner)
                    .Where(u => u != null)
                    .DistinctBy(u => u.Id)
                    .Select(user =>
                    {
                        predictionsByUser.TryGetValue(user.Id, out var prediction);

                        var isCaptain = isKnockout &&
                            captainPicks.TryGetValue(user.Id, out var captainTeamId) &&
                            (match.HomeTeamId == captainTeamId || match.AwayTeamId == captainTeamId);

                        var isPleno = prediction != null && match.IsFinished &&
                            match.HomeScore.HasValue && match.AwayScore.HasValue &&
                            prediction.HomeScorePrediction == match.HomeScore.Value &&
                            prediction.AwayScorePrediction == match.AwayScore.Value;

                        return new GroupPredictionParticipantDto
                        {
                            UserId = user.Id,
                            UserName = user.Name,
                            IsCurrentUser = user.Id == currentUserId,
                            HasPrediction = prediction != null,
                            HomeScorePrediction = prediction?.HomeScorePrediction,
                            AwayScorePrediction = prediction?.AwayScorePrediction,
                            PointsEarned = match.IsFinished && prediction != null
                                ? prediction.PointsEarned +
                                  (sharpShooterDict.TryGetValue((user.Id, match.Id), out var ssPoints)
                                      ? ssPoints : 0)
                                : 0,
                            IsCaptain = isCaptain,
                            IsGoldenGoal = goldenGoalSet.Contains((user.Id, match.Id)),
                            IsSharpShooter = sharpShooterDict.ContainsKey((user.Id, match.Id)),
                            IsPleno = isPleno
                        };
                    });

                var orderedMembers = match.IsFinished
                    ? members
                        .OrderByDescending(x => x.PointsEarned)
                        .ThenBy(x => x.UserName)
                        .ToList()
                    : members
                        .OrderBy(x => x.UserName)
                        .ToList();

                return new MatchGroupPredictionsDto
                {
                    GroupId = group.Id,
                    GroupName = group.Name,
                    Participants = orderedMembers
                };
            })
            .ToList();
    }

    private static TeamDto? ToTeamDto(Team? team)
    {
        if (team == null)
        {
            return null;
        }

        return new TeamDto
        {
            Id = team.Id,
            FifaId = team.FifaId ?? string.Empty,
            Name = team.Name,
            Code = team.Code,
            FlagUrl = team.FlagUrl,
            Group = team.Group
        };
    }

    private static StadiumDto ToStadiumDto(Stadium stadium)
    {
        return new StadiumDto
        {
            Id = stadium.Id,
            FifaId = stadium.FifaId ?? string.Empty,
            Name = stadium.Name,
            City = stadium.City,
            Country = stadium.Country
        };
    }

    /// <summary>
    /// Admin-only: triggers bomb assignment for all started knockout rounds, then returns all bomb matches.
    /// </summary>
    [HttpGet("admin/bomb-matches")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminBombMatches()
    {
        var now = DateTime.UtcNow;

        // Trigger lazy assignment for every knockout round that has already started
        var knockoutRounds = new[]
        {
            WorldCupRoundService.RoundOf32,
            WorldCupRoundService.RoundOf16,
            WorldCupRoundService.QuarterFinals,
            WorldCupRoundService.SemiFinals,
            WorldCupRoundService.FinalRound
        };

        foreach (var roundKey in knockoutRounds)
        {
            await _bombMatchService.EnsureAssignedForRoundAsync(roundKey, now);
        }

        var bombs = await _context.BombMatches
            .AsNoTracking()
            .ToListAsync();

        if (bombs.Count == 0)
            return Ok(new List<object>());

        var matchIds = bombs.Select(b => b.MatchId).ToList();
        var matches = await _context.Matches
            .AsNoTracking()
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => matchIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        var visibleBombs = await _bombMatchService.GetVisibleBombMatchesAsync(now);

        var result = bombs.Select(b =>
        {
            matches.TryGetValue(b.MatchId, out var match);
            return new
            {
                RoundKey = b.RoundKey,
                MatchId = b.MatchId,
                AssignedAt = b.AssignedAt,
                IsRevealedToPlayers = visibleBombs.ContainsKey(b.MatchId),
                Match = match == null ? null : new
                {
                    match.Id,
                    HomeTeam = match.HomeTeam?.Name ?? match.HomePlaceholder,
                    AwayTeam = match.AwayTeam?.Name ?? match.AwayPlaceholder,
                    match.MatchDate,
                    match.IsFinished
                }
            };
        }).ToList();

        return Ok(result);
    }

    private static DateTime NormalizeUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }
}
