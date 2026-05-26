namespace Prode.Api.Entities;

public class Stadium
{
    public int Id { get; set; }

    public string? FifaId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public ICollection<Match> Matches { get; set; }
        = new List<Match>();
}
