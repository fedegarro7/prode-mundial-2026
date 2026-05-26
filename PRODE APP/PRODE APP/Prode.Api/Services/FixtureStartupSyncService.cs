using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;

namespace Prode.Api.Services;

public class FixtureStartupSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FixtureStartupSyncService> _logger;

    public FixtureStartupSyncService(
        IServiceProvider serviceProvider,
        ILogger<FixtureStartupSyncService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    )
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            var context =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(stoppingToken);

            var syncService =
                scope.ServiceProvider
                    .GetRequiredService<FifaFixtureSyncService>();

            await syncService.SyncWorldCup2026Async(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // App shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo sincronizar el fixture FIFA 2026 al iniciar"
            );
        }
    }
}
