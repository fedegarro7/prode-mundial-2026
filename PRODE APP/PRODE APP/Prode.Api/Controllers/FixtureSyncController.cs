using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Api.Services;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/fixture-sync")]
public class FixtureSyncController : ControllerBase
{
    private readonly FifaFixtureSyncService _syncService;

    public FixtureSyncController(
        FifaFixtureSyncService syncService
    )
    {
        _syncService = syncService;
    }

    [HttpPost("fifa-2026")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SyncFifa2026(
        CancellationToken cancellationToken
    )
    {
        var result = await _syncService.SyncWorldCup2026Async(
            cancellationToken
        );

        return Ok(result);
    }
}
