namespace Prode.Api.Entities;

public class Match
{
    public int Id { get; set; }

    public string? FifaId { get; set; }

    public int? MatchNumber { get; set; }

    public int? HomeTeamId { get; set; }

    public Team? HomeTeam { get; set; }

    public string HomePlaceholder { get; set; } = string.Empty;

    public int? AwayTeamId { get; set; }

    public Team? AwayTeam { get; set; }

    public string AwayPlaceholder { get; set; } = string.Empty;

    public DateTime MatchDate { get; set; }

    public string Stage { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public int StadiumId { get; set; }

    public Stadium Stadium { get; set; } = null!;

    public int? HomeScore { get; set; }

    public int? AwayScore { get; set; }

    public bool WasDecidedByPenalties { get; set; }

    public bool IsFinished { get; set; }

    public bool PredictionsLocked { get; set; }

    public ICollection<Prediction> Predictions { get; set; }
        = new List<Prediction>();

    public BombMatch? BombMatch { get; set; }

    public ICollection<GoldenGoalPick> GoldenGoalPicks { get; set; }
        = new List<GoldenGoalPick>();

    public ICollection<SharpShooterPrediction> SharpShooterPredictions { get; set; }
        = new List<SharpShooterPrediction>();
}
