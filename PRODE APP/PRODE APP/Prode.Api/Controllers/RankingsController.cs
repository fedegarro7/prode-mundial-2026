using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.DTOs;

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
}
