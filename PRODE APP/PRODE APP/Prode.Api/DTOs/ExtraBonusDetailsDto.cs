namespace Prode.Api.DTOs;

/// <summary>
/// Represents extra bonus points breakdown by mechanic for a user in a specific round.
/// </summary>
public class ExtraBonusDetailsDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    // Golden Goal: user selected a match and earned bonus for exact score
    public GoldenGoalBonusDto? GoldenGoal { get; set; }

    // Captain: user selected a team and earned bonus for correct result in matches with that team
    public CaptainBonusDto? Captain { get; set; }

    // Sharp Shooter: user earned bonus for specific match predictions
    public List<SharpShooterBonusDto> SharpShooter { get; set; } = [];

    // Oracle: user earned bonus for closest guess on draws/penalties
    public OracleBonusDto? OracleDraws { get; set; }
    public OracleBonusDto? OraclePenalties { get; set; }

    public bool IsRoundKing { get; set; }

    public int TotalExtraPoints =>
        (GoldenGoal?.PointsEarned ?? 0) +
        (Captain?.PointsEarned ?? 0) +
        SharpShooter.Sum(ss => ss.PointsEarned) +
        (OracleDraws?.PointsEarned ?? 0) +
        (OraclePenalties?.PointsEarned ?? 0);
}

public class GoldenGoalBonusDto
{
    public int MatchId { get; set; }
    public string MatchDescription { get; set; } = string.Empty; // "Home vs Away"
    public int PointsEarned { get; set; }
}

public class CaptainBonusDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public List<CaptainMatchContributionDto> Matches { get; set; } = [];
    public int PointsEarned { get; set; }
}

public class CaptainMatchContributionDto
{
    public int MatchId { get; set; }
    public string MatchDescription { get; set; } = string.Empty;
    public int PointsEarned { get; set; }
}

public class SharpShooterBonusDto
{
    public int MatchId { get; set; }
    public string MatchDescription { get; set; } = string.Empty;
    public int PointsEarned { get; set; }
}

public class OracleBonusDto
{
    public string Category { get; set; } = string.Empty; // "Empates" or "Penales"
    public int Prediction { get; set; }
    public int Actual { get; set; }
    public int Distance => Math.Abs(Prediction - Actual);
    public int PointsEarned { get; set; }
    public bool IsWinner { get; set; }
}

/// <summary>
/// Response containing all extra bonuses for a group in a specific round.
/// </summary>
public class RoundExtraBonusesDto
{
    public string RoundKey { get; set; } = string.Empty;
    public string RoundLabel { get; set; } = string.Empty;
    public List<ExtraBonusDetailsDto> Users { get; set; } = [];
}
