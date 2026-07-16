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
public class RankingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RankingsController(AppDbContext context)
    {
        _context = context;
    }

    private static readonly Dictionary<string, string> RoundLabels = new()
    {
        [WorldCupRoundService.GroupStage] = "Fase de Grupos",
        [WorldCupRoundService.RoundOf32] = "Dieciseisavos de Final",
        [WorldCupRoundService.RoundOf16] = "Octavos de Final",
        [WorldCupRoundService.QuarterFinals] = "Cuartos de Final",
        [WorldCupRoundService.SemiFinals] = "Semifinales",
        [WorldCupRoundService.FinalRound] = "Ronda Final",
    };

    private static readonly Dictionary<string, int> RoundBasePoints = new()
    {
        [WorldCupRoundService.GroupStage] = 3,
        [WorldCupRoundService.RoundOf32] = 4,
        [WorldCupRoundService.RoundOf16] = 5,
        [WorldCupRoundService.QuarterFinals] = 7,
        [WorldCupRoundService.SemiFinals] = 10,
        [WorldCupRoundService.FinalRound] = 12,
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
                    + x.SharpShooterPredictions
                        .Sum(p => p.PointsAwarded)
                    + x.RoundAwards
                        .Sum(a => a.PointsAwarded)
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
            ["ROUND_KING"] = "👑 Rey de la Fecha",
            ["ORACLE_DRAWS"] = "🔮 Oráculo (Empates)",
            ["ORACLE_PENALTIES"] = "🔮 Oráculo (Penales)",
        };

        var awards = awardEntries
            .GroupBy(a => a.AwardType)
            .Select(g => new AwardWinnerDto
            {
                AwardType = g.Key,
                AwardLabel = awardLabels.GetValueOrDefault(g.Key, g.Key),
                Winners = g.Select(a => a.User.Name).Distinct().ToList(),
                PointsAwarded = g.First().PointsAwarded,
            })
            .ToList();

        return Ok(new RoundSummaryDto
        {
            RoundKey = roundKey,
            RoundLabel = roundLabel,
            BasePoints = basePoints,
            BombMatch = bombInfo,
            Awards = awards,
        });
    }

    [HttpGet("group/{groupId}/extra-bonus/{roundKey}")]
    [Authorize]
    public async Task<IActionResult> GroupExtraBonus(int groupId, string roundKey)
    {
        var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        // Verify user is member of group OR is admin
        var group = await _context.PrivateGroups
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return NotFound("Grupo no encontrado");

        var isMember = group.OwnerId == userId ||
            group.Memberships.Any(m => m.UserId == userId && m.Status == MembershipStatus.Approved);
        
        var isAdmin = currentUser?.IsAdmin ?? false;

        // Allow access if user is member OR is admin
        if (!isMember && !isAdmin)
            return Forbid();

        var roundLabel = RoundLabels.GetValueOrDefault(roundKey, roundKey);

        // Get all members of the group
        var memberIds = group.Memberships
            .Where(m => m.Status == MembershipStatus.Approved)
            .Select(m => m.UserId)
            .Append(group.OwnerId)
            .Distinct()
            .ToList();

        var members = await _context.Users
            .Where(u => memberIds.Contains(u.Id))
            .ToListAsync();

        // Get all matches in this round
        List<Match> roundMatches;
        if (roundKey == WorldCupRoundService.GroupStage)
        {
            roundMatches = await _context.Matches
                .Where(m => !string.IsNullOrWhiteSpace(m.GroupName))
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .ToListAsync();
        }
        else
        {
            roundMatches = _context.Matches
                .Where(m => string.IsNullOrWhiteSpace(m.GroupName) &&
                            !string.IsNullOrWhiteSpace(m.Stage))
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .AsEnumerable()
                .Where(m => GetRoundKeyFromMatch(m) == roundKey)
                .ToList();
        }

        var matchIds = roundMatches.Select(m => m.Id).ToList();

        // Calculate actual draws (after 90 min) and penalties.
        // A draw after 90 min covers both penalty shootout matches (scores stay tied)
        // and extra-time decisions (WentToExtraTime = true, final score differs).
        var realDraws = roundMatches.Count(m => m.IsFinished &&
            ((m.HomeScore == m.AwayScore) || m.WentToExtraTime));
        var realPenalties = roundMatches.Count(m => m.IsFinished && m.WasDecidedByPenalties);

        // Get all extra bonus data
        var goldenGoals = await _context.GoldenGoalPicks
            .Where(g => g.RoundKey == roundKey && memberIds.Contains(g.UserId))
            .Include(g => g.Match.HomeTeam)
            .Include(g => g.Match.AwayTeam)
            .ToListAsync();

        var captainPicks = await _context.CaptainPicks
            .Where(c => memberIds.Contains(c.UserId))
            .Include(c => c.Team)
            .ToListAsync();

        var sharpShooters = await _context.SharpShooterPredictions
            .Where(s => s.RoundKey == roundKey && memberIds.Contains(s.UserId))
            .Include(s => s.Match.HomeTeam)
            .Include(s => s.Match.AwayTeam)
            .ToListAsync();

        var oracles = await _context.OraclePredictions
            .Where(o => o.RoundKey == roundKey && memberIds.Contains(o.UserId))
            .ToListAsync();

        var oracleAwards = await _context.RoundAwards
            .Where(a => a.RoundKey == roundKey &&
                   (a.AwardType == ScoreRecalculationService.OracleDrawsAward ||
                    a.AwardType == ScoreRecalculationService.OraclePenaltiesAward ||
                    a.AwardType == ScoreRecalculationService.RoundKingAward) &&
                   memberIds.Contains(a.UserId))
            .ToListAsync();

        var predictions = await _context.Predictions
            .Where(p => matchIds.Contains(p.MatchId) && memberIds.Contains(p.UserId))
            .Include(p => p.Match)
            .ToListAsync();

        // Build response
        var bonusDetails = members.Select(member =>
        {
            var details = new ExtraBonusDetailsDto
            {
                UserId = member.Id,
                UserName = member.Name
            };

            // Golden Goal — always show when the user made a pick for this round
            var gg = goldenGoals.FirstOrDefault(g => g.UserId == member.Id);
            if (gg != null)
            {
                var matchPred = predictions.FirstOrDefault(p => p.MatchId == gg.MatchId && p.UserId == member.Id);
                var isExact = false;
                var matchDesc = $"Partido {gg.MatchId}";

                if (gg.Match != null)
                {
                    matchDesc = $"{gg.Match.HomeTeam?.Name ?? gg.Match.HomePlaceholder} vs {gg.Match.AwayTeam?.Name ?? gg.Match.AwayPlaceholder}";
                    isExact = matchPred != null &&
                              matchPred.Match != null &&
                              matchPred.Match.IsFinished &&
                              matchPred.HomeScorePrediction == matchPred.Match.HomeScore &&
                              matchPred.AwayScorePrediction == matchPred.Match.AwayScore;
                }

                details.GoldenGoal = new GoldenGoalBonusDto
                {
                    MatchId = gg.MatchId,
                    MatchDescription = matchDesc,
                    PointsEarned = isExact ? (gg.Match?.HomeScore == gg.Match?.AwayScore ? 12 : 6) : 0
                };
            }

            // Captain — always show when the user has a pick, even if 0 pts this round
            var captain = captainPicks.FirstOrDefault(c => c.UserId == member.Id);
            if (captain != null && captain.Team != null)
            {
                var captainMatches = new List<CaptainMatchContributionDto>();
                var totalCaptainPoints = 0;

                foreach (var match in roundMatches)
                {
                    if (match.IsFinished && (match.HomeTeamId == captain.TeamId || match.AwayTeamId == captain.TeamId))
                    {
                        var pred = predictions.FirstOrDefault(p => p.MatchId == match.Id && p.UserId == member.Id);
                        if (pred != null)
                        {
                            var predictedResult = GetResult(pred.HomeScorePrediction, pred.AwayScorePrediction);
                            var actualResult = GetResult(match.HomeScore ?? 0, match.AwayScore ?? 0);
                            if (predictedResult == actualResult)
                            {
                                var points = 5;
                                captainMatches.Add(new CaptainMatchContributionDto
                                {
                                    MatchId = match.Id,
                                    MatchDescription = $"{match.HomeTeam?.Name ?? match.HomePlaceholder} vs {match.AwayTeam?.Name ?? match.AwayPlaceholder}",
                                    PointsEarned = points
                                });
                                totalCaptainPoints += points;
                            }
                        }
                    }
                }

                details.Captain = new CaptainBonusDto
                {
                    TeamId = captain.TeamId,
                    TeamName = captain.Team.Name,
                    Matches = captainMatches,
                    PointsEarned = totalCaptainPoints
                };
            }

            // Sharp Shooter
            var mySS = sharpShooters.Where(s => s.UserId == member.Id).ToList();
            foreach (var ss in mySS)
            {
                if (ss.Match != null)
                {
                    details.SharpShooter.Add(new SharpShooterBonusDto
                    {
                        MatchId = ss.MatchId,
                        MatchDescription = $"{ss.Match.HomeTeam?.Name ?? ss.Match.HomePlaceholder} vs {ss.Match.AwayTeam?.Name ?? ss.Match.AwayPlaceholder}",
                        PointsEarned = ss.PointsAwarded
                    });
                }
            }

            // Oracle — always show for all users who made predictions, winner or not
            var myOracle = oracles.FirstOrDefault(o => o.UserId == member.Id);
            if (myOracle != null)
            {
                var drawsAward = oracleAwards.FirstOrDefault(a =>
                    a.UserId == member.Id && a.AwardType == ScoreRecalculationService.OracleDrawsAward);
                details.OracleDraws = new OracleBonusDto
                {
                    Category = "Empates",
                    Prediction = myOracle.DrawsAfterNinetyPrediction,
                    Actual = realDraws,
                    PointsEarned = drawsAward?.PointsAwarded ?? 0,
                    IsWinner = drawsAward != null
                };

                var penaltiesAward = oracleAwards.FirstOrDefault(a =>
                    a.UserId == member.Id && a.AwardType == ScoreRecalculationService.OraclePenaltiesAward);
                details.OraclePenalties = new OracleBonusDto
                {
                    Category = "Penales",
                    Prediction = myOracle.PenaltyShootoutsPrediction,
                    Actual = realPenalties,
                    PointsEarned = penaltiesAward?.PointsAwarded ?? 0,
                    IsWinner = penaltiesAward != null
                };
            }

            details.IsRoundKing = oracleAwards.Any(a =>
                a.UserId == member.Id && a.AwardType == ScoreRecalculationService.RoundKingAward);

            return details;
        }).ToList();

        return Ok(new RoundExtraBonusesDto
        {
            RoundKey = roundKey,
            RoundLabel = roundLabel,
            Users = bonusDetails
        });
    }

    private static string GetResult(int homeScore, int awayScore)
    {
        if (homeScore > awayScore) return "HOME";
        if (awayScore > homeScore) return "AWAY";
        return "DRAW";
    }

    private static string GetRoundKeyFromMatch(Match match) => WorldCupRoundService.GetRoundKey(match);
}
