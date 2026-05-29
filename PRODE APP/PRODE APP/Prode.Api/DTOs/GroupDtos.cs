namespace Prode.Api.DTOs;

/// <summary>Payload to create a new private group.</summary>
public record CreateGroupDto(string Name);

/// <summary>Payload to request joining a group via invite code.</summary>
public record JoinGroupDto(string InviteCode);

/// <summary>Summary of a private group returned to the client.</summary>
public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public int MemberCount { get; set; }
    public int PendingRequestCount { get; set; }
    /// <summary>Caller's relationship: "Owner" | "Approved" | "Pending" | "None"</summary>
    public string MembershipStatus { get; set; } = "None";
    public DateTime CreatedAt { get; set; }
}

/// <summary>A pending join-request visible to the group owner.</summary>
public class JoinRequestDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

/// <summary>A member entry within an admin group view.</summary>
public class AdminGroupMemberDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Approved" | "Pending"
}

/// <summary>Full group details returned to admin users.</summary>
public class AdminGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int PendingRequestCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AdminGroupMemberDto> Members { get; set; } = [];
}

/// <summary>Ranking entry within a private group.</summary>
public class GroupRankingDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int Position { get; set; }
    public bool IsCurrentUser { get; set; }
}
