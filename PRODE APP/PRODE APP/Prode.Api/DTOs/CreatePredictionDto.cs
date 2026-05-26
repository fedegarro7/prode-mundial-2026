using System.ComponentModel.DataAnnotations;

namespace Prode.Api.DTOs;

public class CreatePredictionDto
{
    [Required]
    public int MatchId { get; set; }

    [Range(0, 20)]
    public int HomeScorePrediction { get; set; }

    [Range(0, 20)]
    public int AwayScorePrediction { get; set; }
}