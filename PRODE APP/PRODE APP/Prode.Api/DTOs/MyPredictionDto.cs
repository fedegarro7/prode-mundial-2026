namespace Prode.Api.DTOs;

public class MyPredictionDto
{
    public int HomeScorePrediction { get; set; }

    public int AwayScorePrediction { get; set; }

    public int PointsEarned { get; set; }

    public int BasePointsEarned { get; set; }

    public int MultiplierBonusPoints { get; set; }

    public int CaptainBonusPoints { get; set; }
}