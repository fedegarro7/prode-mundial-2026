namespace Prode.Api.Entities;

public class Prediction
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public int MatchId { get; set; }

    public Match Match { get; set; } = null!;

    public int HomeScorePrediction { get; set; }

    public int AwayScorePrediction { get; set; }

    public int BasePointsEarned { get; set; }

    public int MultiplierBonusPoints { get; set; }

    public int CaptainBonusPoints { get; set; }

    public int PointsEarned { get; set; }
}
