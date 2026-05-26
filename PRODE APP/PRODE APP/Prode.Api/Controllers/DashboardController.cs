using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.DTOs;
using Prode.Api.Entities;
using System.Security.Claims;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var stats = new DashboardStatsDto
        {
            TotalUsers =
                await _context.Users.CountAsync(),

            TotalMatches =
                await _context.Matches.CountAsync(),

            FinishedMatches =
                await _context.Matches
                    .CountAsync(x => x.IsFinished),

            TotalPredictions =
                await _context.Predictions.CountAsync(),

            AveragePoints =
                await _context.Predictions
                    .AverageAsync(x =>
                        (double?)x.PointsEarned
                    ) ?? 0
        };

        return Ok(stats);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = Guid.Parse(
            User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value
        );

        var now = DateTime.UtcNow;

        var totalPredictions = await _context.Predictions
            .CountAsync(p => p.UserId == userId);

        var totalPoints = await _context.Predictions
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.PointsEarned);

        var pendingPredictions = await _context.Matches
            .CountAsync(m =>
                m.MatchDate > now &&
                !m.IsFinished &&
                !m.PredictionsLocked &&
                m.HomeTeamId.HasValue &&
                m.AwayTeamId.HasValue &&
                !m.Predictions.Any(p => p.UserId == userId)
            );

        var approvedGroups = await _context.GroupMemberships
            .CountAsync(m =>
                m.UserId == userId &&
                m.Status == MembershipStatus.Approved
            );

        var ranking = await _context.Users
            .Select(u => new
            {
                u.Id,
                Points = u.Predictions.Sum(p => p.PointsEarned),
                u.Name
            })
            .OrderByDescending(u => u.Points)
            .ThenBy(u => u.Name)
            .ToListAsync();

        var position = ranking.FindIndex(u => u.Id == userId);

        var nextPending = await _context.Matches
            .AsNoTracking()
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Stadium)
            .Where(m =>
                m.MatchDate > now &&
                !m.IsFinished &&
                !m.PredictionsLocked &&
                m.HomeTeamId.HasValue &&
                m.AwayTeamId.HasValue &&
                !m.Predictions.Any(p => p.UserId == userId)
            )
            .OrderBy(m => m.MatchDate)
            .FirstOrDefaultAsync();

        return Ok(new MyDashboardDto
        {
            TotalPredictions = totalPredictions,
            PendingPredictions = pendingPredictions,
            TotalPoints = totalPoints,
            GlobalPosition = position >= 0 ? position + 1 : null,
            ApprovedGroups = approvedGroups,
            NextPendingPrediction = nextPending is null
                ? null
                : new PendingPredictionDto
                {
                    MatchId = nextPending.Id,
                    MatchNumber = nextPending.MatchNumber,
                    HomeTeam = ToTeamDto(nextPending.HomeTeam),
                    HomePlaceholder = nextPending.HomePlaceholder,
                    AwayTeam = ToTeamDto(nextPending.AwayTeam),
                    AwayPlaceholder = nextPending.AwayPlaceholder,
                    MatchDate = nextPending.MatchDate,
                    Stage = nextPending.Stage,
                    GroupName = nextPending.GroupName,
                    Stadium = new StadiumDto
                    {
                        Id = nextPending.Stadium.Id,
                        FifaId = nextPending.Stadium.FifaId ?? string.Empty,
                        Name = nextPending.Stadium.Name,
                        City = nextPending.Stadium.City,
                        Country = nextPending.Stadium.Country
                    }
                }
        });
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
}
