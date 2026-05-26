using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.DTOs;
using Prode.Api.Entities;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class StadiumsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StadiumsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var stadiums = await _context.Stadiums
            .AsNoTracking()
            .OrderBy(x => x.Country)
            .ThenBy(x => x.City)
            .ToListAsync();

        return Ok(stadiums.Select(ToDto));
    }

    private static StadiumDto ToDto(Stadium stadium)
    {
        return new StadiumDto
        {
            Id = stadium.Id,
            FifaId = stadium.FifaId ?? string.Empty,
            Name = stadium.Name,
            City = stadium.City,
            Country = stadium.Country
        };
    }
}
