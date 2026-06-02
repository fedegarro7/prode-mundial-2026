namespace Prode.Api.Services;

public sealed class PasswordResetOptions
{
    public int ExpireMinutes { get; set; } = 30;

    public int RequestCooldownMinutes { get; set; } = 10;

    public string? ResetUrl { get; set; }
}
