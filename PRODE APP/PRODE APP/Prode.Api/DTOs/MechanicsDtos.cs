namespace Prode.Api.DTOs;

public class SelectCaptainDto
{
    public int TeamId { get; set; }
}

public class SelectMatchMechanicDto
{
    public int MatchId { get; set; }
}

public class SubmitOraclePredictionDto
{
    public string RoundKey { get; set; } = string.Empty;

    public int DrawsAfterNinetyPrediction { get; set; }

    public int PenaltyShootoutsPrediction { get; set; }
}

public class MechanicsStateDto
{
    public CaptainPickDto? Captain { get; set; }

    public List<GoldenGoalPickDto> GoldenGoals { get; set; } = [];

    public List<SharpShooterPickDto> SharpShooters { get; set; } = [];

    public List<OraclePredictionDto> OraclePredictions { get; set; } = [];
}

public class CaptainPickDto
{
    public int TeamId { get; set; }

    public string TeamName { get; set; } = string.Empty;

    public bool IsLocked { get; set; }
}

public class GoldenGoalPickDto
{
    public string RoundKey { get; set; } = string.Empty;

    public int MatchId { get; set; }
}

public class SharpShooterPickDto
{
    public string RoundKey { get; set; } = string.Empty;

    public int MatchId { get; set; }

    public int PointsAwarded { get; set; }
}

public class OraclePredictionDto
{
    public string RoundKey { get; set; } = string.Empty;

    public int DrawsAfterNinetyPrediction { get; set; }

    public int PenaltyShootoutsPrediction { get; set; }
}
