using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.DTOs;
using Prode.Api.Services;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RankingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RankingsController(AppDbContext context)
    {
        _context = context;
    }

    private static readonly Dictionary<string, string> RoundLabels = new()
    {
        [WorldCupRoundService.GroupStage]    = "Fase de Grupos",
        [WorldCupRoundService.RoundOf32]     = "Dieciseisavos de Final",
        [WorldCupRoundService.RoundOf16]     = "Octavos de Final",
        [WorldCupRoundService.QuarterFinals] = "Cuartos de Final",
        [WorldCupRoundService.SemiFinals]    = "Semifinales",
        [WorldCupRoundService.FinalRound]    = "Ronda Final",
    };

    private static readonly Dictionary<string, int> RoundBasePoints = new()
    {
        [WorldCupRoundService.GroupStage]    = 3,
        [WorldCupRoundService.RoundOf32]     = 4,
        [WorldCupRoundService.RoundOf16]     = 5,
        [WorldCupRoundService.QuarterFinals] = 7,
        [WorldCupRoundService.SemiFinals]    = 10,
        [WorldCupRoundService.FinalRound]    = 12,
    };

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var ranking = await _context.Users
            .Select(x => new RankingDto
            {
                UserId = x.Id,
                Name = x.Name,
                TotalPoints =
                    x.Predictions
                        .Where(p => p.Match.IsFinished)
                        .Sum(p => p.PointsEarned)
            })
            .OrderByDescending(x => x.TotalPoints)
            .ToListAsync();

        return Ok(ranking);
    }

    [HttpGet("round-summary")]
    [Authorize]
    public async Task<IActionResult> RoundSummary()
    {
        var activeMatch = await _context.Matches
            .AsNoTracking()
            .Where(m => !m.IsFinished)
            .OrderBy(m => m.MatchDate)
            .FirstOrDefaultAsync();

        string roundKey;
        if (activeMatch != null)
        {
            roundKey = WorldCupRoundService.GetRoundKey(activeMatch);
        }
        else
        {
            var lastMatch = await _context.Matches
                .AsNoTracking()
                .Where(m => m.IsFinished)
                .OrderByDescending(m => m.MatchDate)
                .FirstOrDefaultAsync();

            roundKey = lastMatch != null
                ? WorldCupRoundService.GetRoundKey(lastMatch)
                : WorldCupRoundService.GroupStage;
        }

        var roundLabel = RoundLabels.GetValueOrDefault(roundKey, roundKey);
        var basePoints = RoundBasePoints.GetValueOrDefault(roundKey, 3);

        BombMatchInfoDto? bombInfo = null;
        var bomb = await _context.BombMatches
            .AsNoTracking()
            .Include(b => b.Match)
                .ThenInclude(m => m.HomeTeam)
            .Include(b => b.Match)
                .ThenInclude(m => m.AwayTeam)
            .Where(b => b.RoundKey == roundKey)
            .FirstOrDefaultAsync();

        if (bomb != null)
        {
            bombInfo = new BombMatchInfoDto
            {
                MatchId = bomb.MatchId,
                HomeTeam = bomb.Match.HomeTeam?.Name ?? bomb.Match.HomePlaceholder,
                AwayTeam = bomb.Match.AwayTeam?.Name ?? bomb.Match.AwayPlaceholder,
            };
        }

        var awardEntries = await _context.RoundAwards
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.RoundKey == roundKey)
            .ToListAsync();

        var awardLabels = new Dictionary<string, string>
        {
            ["ROUND_KING"]       = "👑 Rey de la Fecha",
            ["ORACLE_DRAWS"]     = "🔮 Oráculo (Empates)",
            ["ORACLE_PENALTIES"] = "🔮 Oráculo (Penales)",
        };

        var awards = awardEntries
            .GroupBy(a => a.AwardType)
            .Select(g => new AwardWinnerDto
            {
                AwardType    = g.Key,
                AwardLabel   = awardLabels.GetValueOrDefault(g.Key, g.Key),
                Winners      = g.Select(a => a.User.Name).Distinct().ToList(),
                PointsAwarded = g.First().PointsAwarded,
            })
            .ToList();

        return Ok(new RoundSummaryDto
        {
            RoundKey   = roundKey,
            RoundLabel = roundLabel,
            BasePoints = basePoints,
            BombMatch  = bombInfo,
            Awards     = awards,
        });
    }
}
