namespace Prode.Api.DTOs;

public class MatchDetailsDto
{
    public int Id { get; set; }

    public string FifaId { get; set; } = string.Empty;

    public int? MatchNumber { get; set; }

    public TeamDto? HomeTeam { get; set; }

    public string HomePlaceholder { get; set; } = string.Empty;

    public TeamDto? AwayTeam { get; set; }

    public string AwayPlaceholder { get; set; } = string.Empty;

    public DateTime MatchDate { get; set; }

    public string Stage { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public StadiumDto Stadium { get; set; } = null!;

    public int? HomeScore { get; set; }

    public int? AwayScore { get; set; }

    public bool IsFinished { get; set; }

    public bool PredictionsLocked { get; set; }

    public MyPredictionDto? MyPrediction { get; set; }
}
