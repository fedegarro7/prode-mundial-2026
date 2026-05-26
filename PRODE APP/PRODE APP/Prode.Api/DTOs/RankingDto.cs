namespace Prode.Api.DTOs;

public class RankingDto
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int TotalPoints { get; set; }
}