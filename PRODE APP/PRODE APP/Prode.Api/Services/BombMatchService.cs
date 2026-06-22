using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.Entities;

namespace Prode.Api.Services;

public class BombMatchService
{
    private readonly AppDbContext _context;

    public BombMatchService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ensures a Partido Bomba is assigned for the given knockout round.
    /// Assignment happens once – if one already exists it is returned immediately.
    /// A new assignment is only created after the first match of the round has
    /// its predictions locked or its kick-off time has passed.
    /// </summary>
    public async Task<BombMatch?> EnsureAssignedForRoundAsync(
        string roundKey,
        DateTime now,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _context.BombMatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.RoundKey == roundKey, cancellationToken);

        if (existing != null) return existing;

        var allMatches = await _context.Matches
            .Where(m => !string.IsNullOrWhiteSpace(m.Stage))
            .ToListAsync(cancellationToken);

        var roundMatches = allMatches
            .Where(m => WorldCupRoundService.GetRoundKey(m) == roundKey)
            .OrderBy(m => m.MatchDate)
            .ToList();

        if (roundMatches.Count == 0) return null;

        // Only assign once the round is underway
        var firstMatch = roundMatches.First();
        if (!firstMatch.PredictionsLocked && firstMatch.MatchDate > now)
            return null;

        var selected = roundMatches[Random.Shared.Next(roundMatches.Count)];

        var bombMatch = new BombMatch
        {
            MatchId = selected.Id,
            RoundKey = roundKey,
            AssignedAt = now
        };

        _context.BombMatches.Add(bombMatch);
        await _context.SaveChangesAsync(cancellationToken);

        return bombMatch;
    }

    /// <summary>
    /// Returns all bomb matches that are now visible to players.
    /// A bomb is revealed only once ALL matches in its round have predictions locked
    /// (or have already kicked off), so no player can predict knowing which match is the bomb.
    /// </summary>
    public async Task<Dictionary<int, BombMatch>> GetVisibleBombMatchesAsync(
        DateTime now,
        CancellationToken cancellationToken = default
    )
    {
        var bombMatches = await _context.BombMatches
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (bombMatches.Count == 0) return [];

        var allMatches = await _context.Matches
            .Where(m => !string.IsNullOrWhiteSpace(m.Stage))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<int, BombMatch>();

        foreach (var bomb in bombMatches)
        {
            var roundMatches = allMatches
                .Where(m => WorldCupRoundService.GetRoundKey(m) == bomb.RoundKey)
                .ToList();

            // Reveal only when every match in the round is locked or has started
            var allLocked = roundMatches.Count > 0 &&
                roundMatches.All(m => m.PredictionsLocked || m.MatchDate <= now);

            if (allLocked)
            {
                result[bomb.MatchId] = bomb;
            }
        }

        return result;
    }
}
