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

    private readonly ScoringService _scoringService;

    public MatchesController(
    AppDbContext context,
    ScoringService scoringService
)
    {
        _context = context;

        _scoringService = scoringService;
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

        return Ok(ToMatchDetailsDto(createdMatch, null));
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

        match.IsFinished = true;

        match.PredictionsLocked = true;

        foreach (var prediction in match.Predictions)
        {
            prediction.PointsEarned =
                _scoringService.CalculatePoints(
                    prediction,
                    match
                );
        }

        await _context.SaveChangesAsync();

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

        var response = ToMatchDetailsDto(match, myPrediction);

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
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .Include(x => x.Stadium)
            .Where(x => x.IsFinished || x.MatchDate > DateTime.UtcNow.AddHours(-3))
            .OrderBy(x => x.MatchDate)
            .ToListAsync();

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

                            PointsEarned =
                                match.IsFinished ? p.PointsEarned : 0
                        })
                        .FirstOrDefault()
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
        Prediction? myPrediction
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
            PredictionsLocked = match.PredictionsLocked,

            MyPrediction = myPrediction == null
                ? null
                : new MyPredictionDto
                {
                    HomeScorePrediction =
                        myPrediction.HomeScorePrediction,

                    AwayScorePrediction =
                        myPrediction.AwayScorePrediction,

                    PointsEarned =
                        match.IsFinished ? myPrediction.PointsEarned : 0
                }
        };
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
