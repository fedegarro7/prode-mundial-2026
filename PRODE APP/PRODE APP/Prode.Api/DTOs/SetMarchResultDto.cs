namespace Prode.Api.DTOs;

public class SetMatchResultDto
{
    public int HomeScore { get; set; }

    public int AwayScore { get; set; }

    public bool WasDecidedByPenalties { get; set; }

    /// <summary>True when the match was tied at 90 min and decided in extra time (no penalties).</summary>
    public bool WentToExtraTime { get; set; }
}