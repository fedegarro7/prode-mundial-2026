using System.ComponentModel.DataAnnotations;

namespace Prode.Api.DTOs;

public class CreateMatchDto
{
    public int? MatchNumber { get; set; }

    public int? HomeTeamId { get; set; }

    public string HomePlaceholder { get; set; } = string.Empty;

    public int? AwayTeamId { get; set; }

    public string AwayPlaceholder { get; set; } = string.Empty;

    [Required]
    public DateTime MatchDate { get; set; }

    public string Stage { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    [Required]
    public int StadiumId { get; set; }

    public int? HomeScore { get; set; }

    public int? AwayScore { get; set; }

    public bool IsFinished { get; set; }

    public bool PredictionsLocked { get; set; }
}
