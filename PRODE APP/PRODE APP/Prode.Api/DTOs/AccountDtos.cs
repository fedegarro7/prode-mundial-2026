namespace Prode.Api.DTOs;

public record UpdateNameDto(string Name);

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);

public record ForgotPasswordDto(string Email);

public record ResetPasswordDto(
    string Email,
    string Token,
    string NewPassword
);

public class ForgotPasswordResponseDto
{
    public string Message { get; set; } = string.Empty;
}
