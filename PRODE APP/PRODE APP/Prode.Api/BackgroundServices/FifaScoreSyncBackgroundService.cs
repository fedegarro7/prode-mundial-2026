using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.Services;

namespace Prode.Api.BackgroundServices;

/// <summary>
/// Periodically polls the FIFA API to sync match scores.
/// The interval adapts automatically based on match activity:
///   - 2 min  : a match is in progress or starts within 30 min.
///   - 15 min : there are matches scheduled later today.
///   - 60 min : no matches today (rest day).
/// The live interval can be overridden via <c>FixtureSync:ScoreSyncIntervalMinutes</c>.
/// </summary>
public class FifaScoreSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FifaScoreSyncBackgroundService> _logger;
    private readonly TimeSpan _liveInterval;

    // When matches are scheduled today but none active yet.
    private static readonly TimeSpan IdleMatchDayInterval = TimeSpan.FromMinutes(15);
    // When there are no matches today at all.
    private static readonly TimeSpan RestDayInterval = TimeSpan.FromMinutes(60);

    public FifaScoreSyncBackgroundService(
        IServiceProvider services,
        ILogger<FifaScoreSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;

        var minutes = configuration.GetValue<int>(
            "FixtureSync:ScoreSyncIntervalMinutes", 2);

        _liveInterval = TimeSpan.FromMinutes(Math.Max(1, minutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the startup sync finish first, then start the periodic loop.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        _logger.LogInformation(
            "FIFA score sync background service started — live interval: {Interval}",
            _liveInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncScoresAsync(stoppingToken);
            var next = await ComputeNextIntervalAsync(stoppingToken);
            _logger.LogInformation("Next FIFA sync in {Next}", next);
            await Task.Delay(next, stoppingToken);
        }
    }

    /// <summary>
    /// Determines the next sync interval based on upcoming match schedule.
    /// Uses a single lightweight DB query; falls back to <see cref="IdleMatchDayInterval"/> on error.
    /// </summary>
    private async Task<TimeSpan> ComputeNextIntervalAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var endOfToday = now.Date.AddDays(1);

            // Fetch only the MatchDate of unfinished matches today and in the next 30 min window.
            var upcoming = await db.Matches
                .Where(m => !m.IsFinished && m.MatchDate >= now.AddHours(-3) && m.MatchDate < endOfToday)
                .Select(m => m.MatchDate)
                .ToListAsync(ct);

            // A match is in progress if it started ≤ now (and we haven't marked it finished yet).
            var hasLive = upcoming.Any(d => d <= now);
            if (hasLive) return _liveInterval;

            // A match is about to start in ≤ 30 minutes.
            var hasSoon = upcoming.Any(d => d > now && d <= now.AddMinutes(30));
            if (hasSoon) return _liveInterval;

            // Matches later today but not imminent.
            if (upcoming.Count > 0) return IdleMatchDayInterval;

            // Rest day — let Neon sleep.
            return RestDayInterval;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not compute next sync interval, using idle default");
            return IdleMatchDayInterval;
        }
    }

    private async Task SyncScoresAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var syncService =
                scope.ServiceProvider.GetRequiredService<FifaFixtureSyncService>();

            var result = await syncService.SyncWorldCup2026Async(stoppingToken);

            _logger.LogInformation(
                "FIFA score sync completed — {Matches} matches, {Teams} teams",
                result.Matches,
                result.Teams);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            // Log but do not crash; will retry on next interval.
            _logger.LogWarning(ex, "FIFA score sync failed, will retry in {Interval}", _liveInterval);
        }
    }
}
