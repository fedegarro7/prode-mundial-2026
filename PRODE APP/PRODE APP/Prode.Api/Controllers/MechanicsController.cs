using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Api.DTOs;
using Prode.Api.Services;
using System.Security.Claims;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MechanicsController : ControllerBase
{
    private readonly MechanicsService _mechanicsService;

    public MechanicsController(MechanicsService mechanicsService)
    {
        _mechanicsService = mechanicsService;
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> GetState(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var state = await _mechanicsService.GetStateAsync(userId, ct);
        return Ok(state);
    }

    [HttpPost("captain")]
    public async Task<IActionResult> SelectCaptain(
        [FromBody] SelectCaptainDto dto,
        CancellationToken ct
    )
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        try
        {
            await _mechanicsService.SelectCaptainAsync(userId, dto.TeamId, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("golden-goal")]
    public async Task<IActionResult> SelectGoldenGoal(
        [FromBody] SelectMatchMechanicDto dto,
        CancellationToken ct
    )
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        try
        {
            await _mechanicsService.SelectGoldenGoalAsync(userId, dto.MatchId, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sharp-shooter")]
    public async Task<IActionResult> SelectSharpShooter(
        [FromBody] SelectMatchMechanicDto dto,
        CancellationToken ct
    )
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        try
        {
            await _mechanicsService.SelectSharpShooterAsync(userId, dto.MatchId, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("oracle")]
    public async Task<IActionResult> SubmitOracle(
        [FromBody] SubmitOraclePredictionDto dto,
        CancellationToken ct
    )
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        try
        {
            await _mechanicsService.SubmitOraclePredictionAsync(userId, dto, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
