namespace Prode.Api.Entities;

public class OraclePrediction
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string RoundKey { get; set; } = string.Empty;

    public int DrawsAfterNinetyPrediction { get; set; }

    public int PenaltyShootoutsPrediction { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
