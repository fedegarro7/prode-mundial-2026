namespace Prode.Api.Entities;

public class RoundAward
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string RoundKey { get; set; } = string.Empty;

    public string AwardType { get; set; } = string.Empty;

    public int PointsAwarded { get; set; }

    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
