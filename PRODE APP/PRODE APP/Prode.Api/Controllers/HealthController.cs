using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;

    public HealthController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lightweight ping used by Render health checks and UptimeRobot.
    /// Does NOT touch the database so Neon can scale to zero when idle.
    /// </summary>
    [HttpGet]
    [HttpHead]
    public IActionResult Get() =>
        Ok(new { status = "ok", utc = DateTime.UtcNow });

    /// <summary>
    /// Deep health check that verifies DB connectivity. Call manually when needed.
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> GetDb()
    {
        var canConnect = await _context.Database.CanConnectAsync();

        return canConnect
            ? Ok(new { status = "ok", database = "ok", utc = DateTime.UtcNow })
            : StatusCode(503, new { status = "degraded", database = "unavailable" });
    }
}
