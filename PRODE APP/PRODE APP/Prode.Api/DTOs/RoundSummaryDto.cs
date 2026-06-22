namespace Prode.Api.DTOs;

public class RoundSummaryDto
{
    public string RoundKey { get; set; } = string.Empty;
    public string RoundLabel { get; set; } = string.Empty;
    public int BasePoints { get; set; }
    public BombMatchInfoDto? BombMatch { get; set; }
    public List<AwardWinnerDto> Awards { get; set; } = [];
}

public class BombMatchInfoDto
{
    public int MatchId { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
}

public class AwardWinnerDto
{
    public string AwardType { get; set; } = string.Empty;
    public string AwardLabel { get; set; } = string.Empty;
    public List<string> Winners { get; set; } = [];
    public int PointsAwarded { get; set; }
}
