namespace Prode.Api.Entities;

public class CaptainPick
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public int TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LockedAt { get; set; }
}
