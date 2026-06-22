namespace Prode.Api.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public DateTime? PasswordResetRequestedAt { get; set; }

    public bool IsAdmin { get; set; }

    public int TotalPoints { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();

    public ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();

    public ICollection<PrivateGroup> OwnedGroups { get; set; } = new List<PrivateGroup>();

    public ICollection<GoldenGoalPick> GoldenGoalPicks { get; set; } = new List<GoldenGoalPick>();

    public CaptainPick? CaptainPick { get; set; }

    public ICollection<SharpShooterPrediction> SharpShooterPredictions { get; set; } = new List<SharpShooterPrediction>();

    public ICollection<OraclePrediction> OraclePredictions { get; set; } = new List<OraclePrediction>();

    public ICollection<RoundAward> RoundAwards { get; set; } = new List<RoundAward>();
}
