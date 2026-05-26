namespace Prode.Api.DTOs;

public class TeamDto
{
    public int Id { get; set; }

    public string FifaId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string FlagUrl { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;
}
