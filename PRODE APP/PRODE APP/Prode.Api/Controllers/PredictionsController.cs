using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.DTOs;
using Prode.Api.Entities;
using Prode.Api.Services;
using System.Security.Claims;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PredictionsController : ControllerBase
{
    private readonly PredictionService _predictionService;
    private readonly AppDbContext _context;

    public PredictionsController(
        PredictionService predictionService,
        AppDbContext context
    )
    {
        _predictionService = predictionService;
        _context = context;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

    [HttpPost]
    public async Task<IActionResult> Create(CreatePredictionDto dto)
    {
        var result = await _predictionService
            .CreatePrediction(dto, User);

        return Ok(new { message = result });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var userId = CurrentUserId;

        var predictions = await _context.Predictions
            .AsNoTracking()
            .Include(x => x.Match)
                .ThenInclude(x => x.HomeTeam)
            .Include(x => x.Match)
                .ThenInclude(x => x.AwayTeam)
            .Where(x => x.UserId == userId)
            .ToListAsync();

        var response = predictions
            .Select(x => new PredictionResponseDto
            {
                MatchId = x.MatchId,
                HomeTeam = ToTeamDto(x.Match.HomeTeam),
                HomePlaceholder = x.Match.HomePlaceholder,
                AwayTeam = ToTeamDto(x.Match.AwayTeam),
                AwayPlaceholder = x.Match.AwayPlaceholder,
                HomeScorePrediction = x.HomeScorePrediction,
                AwayScorePrediction = x.AwayScorePrediction,
                MatchDate = x.Match.MatchDate
            })
            .ToList();

        return Ok(response);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> Pending([FromQuery] int limit = 8)
    {
        var userId = CurrentUserId;
        var now = DateTime.UtcNow;
        var take = Math.Clamp(limit, 1, 24);

        var matches = await _context.Matches
            .AsNoTracking()
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .Include(x => x.Stadium)
            .Where(x =>
                x.MatchDate > now &&
                !x.IsFinished &&
                !x.PredictionsLocked &&
                x.HomeTeamId.HasValue &&
                x.AwayTeamId.HasValue &&
                !x.Predictions.Any(p => p.UserId == userId)
            )
            .OrderBy(x => x.MatchDate)
            .Take(take)
            .ToListAsync();

        return Ok(matches.Select(ToPendingDto).ToList());
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int limit = 20)
    {
        var userId = CurrentUserId;
        var take = Math.Clamp(limit, 1, 100);

        var predictions = await _context.Predictions
            .AsNoTracking()
            .Include(x => x.Match)
                .ThenInclude(x => x.HomeTeam)
            .Include(x => x.Match)
                .ThenInclude(x => x.AwayTeam)
            .Where(x => x.UserId == userId && x.Match.IsFinished)
            .OrderByDescending(x => x.Match.MatchDate)
            .Take(take)
            .ToListAsync();

        var response = predictions
            .Select(x => new PredictionHistoryDto
            {
                MatchId = x.MatchId,
                MatchNumber = x.Match.MatchNumber,
                HomeTeam = ToTeamDto(x.Match.HomeTeam),
                HomePlaceholder = x.Match.HomePlaceholder,
                AwayTeam = ToTeamDto(x.Match.AwayTeam),
                AwayPlaceholder = x.Match.AwayPlaceholder,
                MatchDate = x.Match.MatchDate,
                Stage = x.Match.Stage,
                HomeScorePrediction = x.HomeScorePrediction,
                AwayScorePrediction = x.AwayScorePrediction,
                HomeScore = x.Match.HomeScore,
                AwayScore = x.Match.AwayScore,
                PointsEarned = x.PointsEarned
            })
            .ToList();

        return Ok(response);
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

    private static PendingPredictionDto ToPendingDto(Match match)
    {
        return new PendingPredictionDto
        {
            MatchId = match.Id,
            MatchNumber = match.MatchNumber,
            HomeTeam = ToTeamDto(match.HomeTeam),
            HomePlaceholder = match.HomePlaceholder,
            AwayTeam = ToTeamDto(match.AwayTeam),
            AwayPlaceholder = match.AwayPlaceholder,
            MatchDate = match.MatchDate,
            Stage = match.Stage,
            GroupName = match.GroupName,
            Stadium = new StadiumDto
            {
                Id = match.Stadium.Id,
                FifaId = match.Stadium.FifaId ?? string.Empty,
                Name = match.Stadium.Name,
                City = match.Stadium.City,
                Country = match.Stadium.Country
            }
        };
    }
}
