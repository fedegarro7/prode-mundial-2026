namespace Prode.Api.Entities;

public class SharpShooterPrediction
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public int MatchId { get; set; }

    public Match Match { get; set; } = null!;

    public string RoundKey { get; set; } = string.Empty;

    public int PointsAwarded { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
