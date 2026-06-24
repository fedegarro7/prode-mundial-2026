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

// ── Round Context (for the interactive mechanics selection page) ──────────────

public class RoundContextDto
{
    public bool IsCaptainLocked { get; set; }

    public List<RoundTeamDto> CaptainTeams { get; set; } = [];

    public List<RoundInfoDto> Rounds { get; set; } = [];
}

public class RoundInfoDto
{
    public string RoundKey { get; set; } = string.Empty;

    public string RoundLabel { get; set; } = string.Empty;

    public bool IsLocked { get; set; }

    public int MatchCount { get; set; }

    public List<RoundMatchDto> Matches { get; set; } = [];
}

public class RoundMatchDto
{
    public int Id { get; set; }

    public string HomeTeam { get; set; } = string.Empty;

    public string AwayTeam { get; set; } = string.Empty;

    public DateTime MatchDate { get; set; }

    public bool IsLocked { get; set; }
}

public class RoundTeamDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FlagUrl { get; set; } = string.Empty;
}
