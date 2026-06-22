namespace Prode.Api.DTOs;

public class SetMatchResultDto
{
    public int HomeScore { get; set; }

    public int AwayScore { get; set; }

    public bool WasDecidedByPenalties { get; set; }
}