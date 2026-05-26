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
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TeamsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var teams = await _context.Teams
            .AsNoTracking()
            .OrderBy(x => x.Group)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(teams.Select(ToDto));
    }

    private static TeamDto ToDto(Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            FifaId = team.FifaId ?? string.Empty,
            Name = team.Name,
            Code = team.Code,
            FlagUrl = team.FlagUrl,
            Group = team.Group
        };
    }
}
