namespace Prode.Api.Entities;

public class Team
{
    public int Id { get; set; }

    public string? FifaId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string FlagUrl { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public ICollection<Match> HomeMatches { get; set; }
        = new List<Match>();

    public ICollection<Match> AwayMatches { get; set; }
        = new List<Match>();
}
