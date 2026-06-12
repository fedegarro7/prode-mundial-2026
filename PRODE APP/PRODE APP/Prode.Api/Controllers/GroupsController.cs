using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.DTOs;
using Prode.Api.Entities;

namespace Prode.Api.Controllers;

/// <summary>
/// Manages private prediction groups (family, office, friends…).
/// Join requests require owner approval before the user can see the group ranking.
/// All endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("Groups")]
public class GroupsController : ControllerBase
{
    private readonly AppDbContext _context;

    public GroupsController(AppDbContext context) => _context = context;

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Guid CurrentUserId =>
        Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

    /// <summary>Generates a random 6-character alphanumeric invite code.</summary>
    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private static GroupDto MapGroup(PrivateGroup g, Guid callerId)
    {
        var approved = g.Memberships.Where(m => m.Status == MembershipStatus.Approved).ToList();
        var pending = g.Memberships.Where(m => m.Status == MembershipStatus.Pending).ToList();

        string status;
        if (g.OwnerId == callerId) status = "Owner";
        else if (approved.Any(m => m.UserId == callerId)) status = "Approved";
        else if (pending.Any(m => m.UserId == callerId)) status = "Pending";
        else status = "None";

        return new GroupDto
        {
            Id = g.Id,
            Name = g.Name,
            InviteCode = g.InviteCode,
            OwnerName = g.Owner?.Name ?? "",
            IsOwner = g.OwnerId == callerId,
            MemberCount = approved.Count,
            PendingRequestCount = g.OwnerId == callerId ? pending.Count : 0,
            MembershipStatus = status,
            CreatedAt = g.CreatedAt
        };
    }

    private async Task<List<GroupRankingDto>> BuildRankingsAsync(PrivateGroup group, Guid callerId)
    {
        // Include the owner in rankings even if they have no explicit membership row.
        var approvedUserIds = group.Memberships
            .Where(m => m.Status == MembershipStatus.Approved)
            .Select(m => m.UserId)
            .Append(group.OwnerId)
            .Distinct()
            .ToList();

        var rankings = await _context.Users
            .Where(u => approvedUserIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Name,
                Points = u.Predictions
                    .Where(p => p.Match.IsFinished)
                    .Sum(p => p.PointsEarned)
            })
            .ToListAsync();

        return rankings
            .OrderByDescending(r => r.Points)
            .ThenBy(r => r.Name)
            .Select((r, i) => new GroupRankingDto
            {
                UserId = r.Id,
                UserName = r.Name,
                TotalPoints = r.Points,
                Position = i + 1,
                IsCurrentUser = r.Id == callerId
            })
            .ToList();
    }

    // ── GET /api/groups ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all groups the current user can access:
    /// owned groups plus groups where the user has an Approved membership.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = CurrentUserId;

        // Owned groups (always visible to the owner)
        var owned = await _context.PrivateGroups
            .Where(g => g.OwnerId == userId)
            .Include(g => g.Owner)
            .Include(g => g.Memberships)
            .ToListAsync();

        // Groups where the user is already an approved member.
        var memberOf = await _context.GroupMemberships
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Approved)
            .Include(m => m.Group)
                .ThenInclude(g => g.Owner)
            .Include(m => m.Group)
                .ThenInclude(g => g.Memberships)
            .Select(m => m.Group)
            .ToListAsync();

        var all = owned
            .Union(memberOf, EqualityComparer<PrivateGroup>.Default)
            .DistinctBy(g => g.Id)
            .Select(g => MapGroup(g, userId))
            .OrderBy(g => g.Name)
            .ToList();

        return Ok(all);
    }

    // ── GET /api/groups/{id} ───────────────────────────────────────────────────

    /// <summary>Returns a single group by ID (must be owner or approved member).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = CurrentUserId;
        var group = await _context.PrivateGroups
            .Include(g => g.Owner)
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null) return NotFound();

        var isOwner = group.OwnerId == userId;
        var membership = group.Memberships.FirstOrDefault(m => m.UserId == userId);

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && !isOwner && (membership is null || membership.Status != MembershipStatus.Approved))
            return Forbid();

        return Ok(MapGroup(group, userId));
    }

    // ── GET /api/groups/{id}/rankings ──────────────────────────────────────────

    /// <summary>Returns the ranking for all approved members of a group.</summary>
    [HttpGet("{id:int}/rankings")]
    public async Task<IActionResult> GetGroupRankings(int id)
    {
        var userId = CurrentUserId;

        var group = await _context.PrivateGroups
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null) return NotFound();

        var isOwner = group.OwnerId == userId;
        var membership = group.Memberships.FirstOrDefault(m => m.UserId == userId);

        if (!isOwner && (membership is null || membership.Status != MembershipStatus.Approved))
            return Forbid();

        return Ok(await BuildRankingsAsync(group, userId));
    }

    // ── GET /api/groups/{id}/requests ──────────────────────────────────────────

    /// <summary>Returns all pending join requests for a group. Owner only.</summary>
    [HttpGet("{id:int}/requests")]
    public async Task<IActionResult> GetRequests(int id)
    {
        var userId = CurrentUserId;

        var group = await _context.PrivateGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (group is null) return NotFound();
        if (group.OwnerId != userId) return Forbid();

        var requests = await _context.GroupMemberships
            .Where(m => m.GroupId == id && m.Status == MembershipStatus.Pending)
            .Include(m => m.User)
            .Select(m => new JoinRequestDto
            {
                UserId = m.UserId,
                UserName = m.User.Name,
                RequestedAt = m.RequestedAt
            })
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

        return Ok(requests);
    }

    // ── POST /api/groups ───────────────────────────────────────────────────────

    /// <summary>Creates a new group. The creator is automatically an approved member.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 50)
            return BadRequest("El nombre del grupo debe tener entre 1 y 50 caracteres.");

        var userId = CurrentUserId;

        // Unique invite code
        string inviteCode;
        do { inviteCode = GenerateInviteCode(); }
        while (await _context.PrivateGroups.AnyAsync(g => g.InviteCode == inviteCode));

        var group = new PrivateGroup
        {
            Name = dto.Name.Trim(),
            InviteCode = inviteCode,
            OwnerId = userId
        };
        _context.PrivateGroups.Add(group);
        await _context.SaveChangesAsync();

        // Creator is auto-approved
        _context.GroupMemberships.Add(new GroupMembership
        {
            GroupId = group.Id,
            UserId = userId,
            Status = MembershipStatus.Approved,
            RequestedAt = DateTime.UtcNow,
            ReviewedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var owner = await _context.Users.FindAsync(userId);

        return CreatedAtAction(nameof(GetById), new { id = group.Id }, new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            InviteCode = group.InviteCode,
            OwnerName = owner?.Name ?? "",
            IsOwner = true,
            MemberCount = 1,
            PendingRequestCount = 0,
            MembershipStatus = "Owner",
            CreatedAt = group.CreatedAt
        });
    }

    // ── POST /api/groups/join ──────────────────────────────────────────────────

    /// <summary>
    /// Submits a join request for a group identified by invite code.
    /// The request is Pending until the owner approves it.
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.InviteCode))
            return BadRequest("Código inválido.");

        var group = await _context.PrivateGroups
            .Include(g => g.Owner)
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.InviteCode == dto.InviteCode.Trim().ToUpperInvariant());

        if (group is null) return NotFound("Código de invitación no encontrado.");

        var userId = CurrentUserId;

        if (group.OwnerId == userId)
            return Conflict("Ya sos el dueño de este grupo.");

        var existing = group.Memberships.FirstOrDefault(m => m.UserId == userId);
        if (existing is not null)
        {
            return existing.Status switch
            {
                MembershipStatus.Approved => Conflict("Ya sos miembro aprobado de este grupo."),
                MembershipStatus.Pending => Conflict("Ya tenés una solicitud pendiente de aprobación."),
                MembershipStatus.Rejected => Conflict("Tu solicitud fue rechazada por el administrador del grupo."),
                _ => Conflict("Ya tenés una solicitud para este grupo.")
            };
        }

        _context.GroupMemberships.Add(new GroupMembership
        {
            GroupId = group.Id,
            UserId = userId,
            Status = MembershipStatus.Pending,
            RequestedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Ok(new { message = "Solicitud enviada. El dueño del grupo debe aprobarla.", groupName = group.Name });
    }

    // ── POST /api/groups/{id}/requests/{userId}/approve ────────────────────────

    /// <summary>Approves a pending join request. Owner only.</summary>
    [HttpPost("{id:int}/requests/{requestUserId:guid}/approve")]
    public async Task<IActionResult> ApproveRequest(int id, Guid requestUserId)
    {
        var ownerId = CurrentUserId;
        var group = await _context.PrivateGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (group is null) return NotFound();
        if (group.OwnerId != ownerId) return Forbid();

        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == requestUserId);

        if (membership is null || membership.Status != MembershipStatus.Pending)
            return NotFound("Solicitud pendiente no encontrada.");

        membership.Status = MembershipStatus.Approved;
        membership.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ── POST /api/groups/{id}/requests/{userId}/reject ─────────────────────────

    /// <summary>Rejects a pending join request. Owner only.</summary>
    [HttpPost("{id:int}/requests/{requestUserId:guid}/reject")]
    public async Task<IActionResult> RejectRequest(int id, Guid requestUserId)
    {
        var ownerId = CurrentUserId;
        var group = await _context.PrivateGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (group is null) return NotFound();
        if (group.OwnerId != ownerId) return Forbid();

        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == requestUserId);

        if (membership is null || membership.Status != MembershipStatus.Pending)
            return NotFound("Solicitud pendiente no encontrada.");

        membership.Status = MembershipStatus.Rejected;
        membership.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ── DELETE /api/groups/{id}/leave ──────────────────────────────────────────

    /// <summary>Leaves a group. The owner must delete the group instead.</summary>
    [HttpDelete("{id:int}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = CurrentUserId;

        var membership = await _context.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == userId);

        if (membership is null) return NotFound();

        if (membership.Group.OwnerId == userId)
            return BadRequest("El creador del grupo no puede abandonarlo. Eliminá el grupo.");

        _context.GroupMemberships.Remove(membership);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ── DELETE /api/groups/{id} ────────────────────────────────────────────────

    /// <summary>Deletes a group and all its memberships. Owner only.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;

        var group = await _context.PrivateGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);

        if (group is null) return NotFound();

        _context.PrivateGroups.Remove(group); // Cascade deletes memberships.
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ── GET /api/groups/admin/all ──────────────────────────────────────────────

    /// <summary>Returns all groups with their members. Admin only.</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllGroupsAdmin()
    {
        var userId = CurrentUserId;

        var groups = await _context.PrivateGroups
            .Include(g => g.Owner)
            .Include(g => g.Memberships)
                .ThenInclude(m => m.User)
            .OrderBy(g => g.Name)
            .ToListAsync();

        var result = new List<AdminGroupDto>();

        foreach (var g in groups)
        {
            var approved = g.Memberships.Where(m => m.Status == MembershipStatus.Approved).ToList();
            var pending = g.Memberships.Where(m => m.Status == MembershipStatus.Pending).ToList();

            var members = approved
                .Select(m => new AdminGroupMemberDto { UserId = m.UserId, UserName = m.User?.Name ?? "", Status = "Approved" })
                .Concat(pending
                    .Select(m => new AdminGroupMemberDto { UserId = m.UserId, UserName = m.User?.Name ?? "", Status = "Pending" }))
                .OrderBy(m => m.UserName)
                .ToList();

            result.Add(new AdminGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                InviteCode = g.InviteCode,
                OwnerName = g.Owner?.Name ?? "",
                MemberCount = approved.Count,
                PendingRequestCount = pending.Count,
                CreatedAt = g.CreatedAt,
                Rankings = await BuildRankingsAsync(g, userId),
                Members = members
            });
        }

        return Ok(result);
    }
}
