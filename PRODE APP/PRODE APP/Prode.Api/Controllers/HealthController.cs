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

    [HttpGet]
    [HttpHead]
    public async Task<IActionResult> Get()
    {
        var canConnect = await _context.Database.CanConnectAsync();

        return canConnect
            ? Ok(new { status = "ok", database = "ok", utc = DateTime.UtcNow })
            : StatusCode(503, new { status = "degraded", database = "unavailable" });
    }
}
