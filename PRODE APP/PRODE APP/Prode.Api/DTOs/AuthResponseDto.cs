namespace Prode.Api.DTOs;

public class AuthResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public string Token { get; set; } = string.Empty;
}
