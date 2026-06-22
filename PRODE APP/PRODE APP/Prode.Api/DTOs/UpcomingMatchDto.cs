namespace Prode.Api.DTOs;

public class UpcomingMatchDto
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

    public bool PredictionsLocked { get; set; }

    public int? HomeScore { get; set; }

    public int? AwayScore { get; set; }

    public bool IsFinished { get; set; }

    public bool IsBombMatch { get; set; }

    public MyPredictionDto? MyPrediction { get; set; }

    public List<MatchGroupPredictionsDto> GroupPredictions { get; set; } = [];
}

public class MatchGroupPredictionsDto
{
    public int GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public List<GroupPredictionParticipantDto> Participants { get; set; } = [];
}

public class GroupPredictionParticipantDto
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public bool IsCurrentUser { get; set; }

    public bool HasPrediction { get; set; }

    public int? HomeScorePrediction { get; set; }

    public int? AwayScorePrediction { get; set; }

    public int PointsEarned { get; set; }
}
