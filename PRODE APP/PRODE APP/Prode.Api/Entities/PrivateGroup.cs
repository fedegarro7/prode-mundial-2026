namespace Prode.Api.Entities;

/// <summary>A private prediction group (e.g. family, office, friends).</summary>
public class PrivateGroup
{
    public int Id { get; set; }

    /// <summary>Display name of the group.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short unique invite code players use to join.</summary>
    public string InviteCode { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMembership> Memberships { get; set; }
        = new List<GroupMembership>();
}
