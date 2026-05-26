namespace Prode.Api.DTOs;

public class StadiumDto
{
    public int Id { get; set; }

    public string FifaId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}
