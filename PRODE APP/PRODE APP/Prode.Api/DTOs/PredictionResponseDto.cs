namespace Prode.Api.DTOs;

public class PredictionResponseDto
{
    public int MatchId { get; set; }

    public TeamDto? HomeTeam { get; set; }

    public string HomePlaceholder { get; set; } = string.Empty;

    public TeamDto? AwayTeam { get; set; }

    public string AwayPlaceholder { get; set; } = string.Empty;

    public int HomeScorePrediction { get; set; }

    public int AwayScorePrediction { get; set; }

    public DateTime MatchDate { get; set; }
}
