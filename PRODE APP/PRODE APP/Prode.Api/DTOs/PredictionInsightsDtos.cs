namespace Prode.Api.DTOs;

public class PendingPredictionDto
{
    public int MatchId { get; set; }

    public int? MatchNumber { get; set; }

    public TeamDto? HomeTeam { get; set; }

    public string HomePlaceholder { get; set; } = string.Empty;

    public TeamDto? AwayTeam { get; set; }

    public string AwayPlaceholder { get; set; } = string.Empty;

    public DateTime MatchDate { get; set; }

    public string Stage { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public StadiumDto Stadium { get; set; } = null!;
}

public class PredictionHistoryDto
{
    public int MatchId { get; set; }

    public int? MatchNumber { get; set; }

    public TeamDto? HomeTeam { get; set; }

    public string HomePlaceholder { get; set; } = string.Empty;

    public TeamDto? AwayTeam { get; set; }

    public string AwayPlaceholder { get; set; } = string.Empty;

    public DateTime MatchDate { get; set; }

    public string Stage { get; set; } = string.Empty;

    public int HomeScorePrediction { get; set; }

    public int AwayScorePrediction { get; set; }

    public int? HomeScore { get; set; }

    public int? AwayScore { get; set; }

    public int PointsEarned { get; set; }
}

public class MyDashboardDto
{
    public int TotalPredictions { get; set; }

    public int PendingPredictions { get; set; }

    public int TotalPoints { get; set; }

    public int? GlobalPosition { get; set; }

    public int ApprovedGroups { get; set; }

    public PendingPredictionDto? NextPendingPrediction { get; set; }
}
