using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Prode.Api.Data;
using Prode.Api.DTOs;
using Prode.Api.Entities;
using Prode.Api.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly PasswordResetOptions _passwordResetOptions;
    private readonly ILogger<AuthController> _logger;

    private const string PasswordResetMessage =
        "Si el email existe, enviamos instrucciones para recuperar la cuenta.";

    public AuthController(
        AppDbContext context,
        JwtService jwtService,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IEmailSender emailSender,
        IOptions<PasswordResetOptions> passwordResetOptions,
        ILogger<AuthController> logger
    )
    {
        _context = context;
        _jwtService = jwtService;
        _environment = environment;
        _configuration = configuration;
        _emailSender = emailSender;
        _passwordResetOptions = passwordResetOptions.Value;
        _logger = logger;
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthSensitive")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest("El nombre es obligatorio");
        }

        if (!IsValidPassword(dto.Password))
        {
            return BadRequest("La contraseña debe tener al menos 8 caracteres");
        }

        var email = NormalizeEmail(dto.Email);

        var exists = await _context.Users
            .AnyAsync(x => x.Email == email);

        if (exists)
        {
            return BadRequest("El email ya existe");
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return Ok(CreateAuthResponse(user, token));
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthSensitive")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = NormalizeEmail(dto.Email);

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
        {
            return Unauthorized("Credenciales invalidas");
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(
            dto.Password,
            user.PasswordHash
        );

        if (!validPassword)
        {
            return Unauthorized("Credenciales invalidas");
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(CreateAuthResponse(user, token));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!IsValidPassword(dto.NewPassword))
        {
            return BadRequest("La nueva contraseña debe tener al menos 8 caracteres");
        }

        var userId = Guid.Parse(
            User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value
        );

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return Unauthorized();
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(
            dto.CurrentPassword,
            user.PasswordHash
        );

        if (!validPassword)
        {
            return BadRequest("La contraseña actual no es correcta");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        user.PasswordResetRequestedAt = null;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("PasswordRecovery")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user is not null)
        {
            var now = DateTime.UtcNow;
            var cooldownMinutes = Math.Max(
                1,
                _passwordResetOptions.RequestCooldownMinutes
            );

            if (
                user.PasswordResetRequestedAt is null ||
                user.PasswordResetRequestedAt.Value
                    .AddMinutes(cooldownMinutes) <= now
            )
            {
                var token = CreateResetToken();
                var expiresAt = now.AddMinutes(
                    Math.Clamp(_passwordResetOptions.ExpireMinutes, 5, 120)
                );

                user.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword(token);
                user.PasswordResetTokenExpiresAt = expiresAt;
                user.PasswordResetRequestedAt = now;

                await _context.SaveChangesAsync();

                try
                {
                    await _emailSender.SendPasswordResetAsync(
                        user.Email,
                        token,
                        expiresAt,
                        HttpContext.RequestAborted
                    );
                }
                catch (Exception ex)
                {
                    user.PasswordResetTokenHash = null;
                    user.PasswordResetTokenExpiresAt = null;
                    user.PasswordResetRequestedAt = null;

                    await _context.SaveChangesAsync();

                    _logger.LogError(
                        ex,
                        "Could not send password reset email for user {UserId}.",
                        user.Id
                    );
                }
            }
        }

        return Ok(new ForgotPasswordResponseDto
        {
            Message = PasswordResetMessage
        });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("PasswordRecovery")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (!IsValidPassword(dto.NewPassword))
        {
            return BadRequest("La nueva contraseña debe tener al menos 8 caracteres");
        }

        var email = NormalizeEmail(dto.Email);
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (
            user is null ||
            string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) ||
            user.PasswordResetTokenExpiresAt is null ||
            user.PasswordResetTokenExpiresAt < DateTime.UtcNow ||
            !BCrypt.Net.BCrypt.Verify(dto.Token, user.PasswordResetTokenHash)
        )
        {
            return BadRequest("Token invalido o vencido");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        user.PasswordResetRequestedAt = null;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(
            AuthCookieDefaults.CookieName,
            CreateAuthCookieOptions()
        );

        return NoContent();
    }

    [Authorize]
    [HttpPut("name")]
    public async Task<IActionResult> UpdateName([FromBody] UpdateNameDto dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 60)
            return BadRequest("El nombre debe tener entre 1 y 60 caracteres.");

        var userId = Guid.Parse(
            User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value
        );

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null) return Unauthorized();

        user.Name = name;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return Ok(CreateAuthResponse(user, token));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = Guid.Parse(
            User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value
        );

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null) return Unauthorized();

        var token = _jwtService.GenerateToken(user);

        return Ok(CreateAuthResponse(user, token));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static bool IsValidPassword(string password)
    {
        return !string.IsNullOrWhiteSpace(password) && password.Length >= 8;
    }

    private static string CreateResetToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static AuthResponseDto CreateAuthResponse(User user, string token)
    {
        return new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            Token = token
        };
    }

    private void SetAuthCookie(string token)
    {
        var options = CreateAuthCookieOptions();
        options.Expires = DateTimeOffset.UtcNow.AddMinutes(GetJwtExpireMinutes());

        Response.Cookies.Append(
            AuthCookieDefaults.CookieName,
            token,
            options
        );
    }

    private CookieOptions CreateAuthCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = _environment.IsDevelopment()
                ? SameSiteMode.Lax
                : SameSiteMode.None,
            Path = "/"
        };
    }

    private int GetJwtExpireMinutes()
    {
        return int.TryParse(_configuration["Jwt:ExpireMinutes"], out var minutes)
            ? Math.Clamp(minutes, 5, 24 * 60)
            : 120;
    }
}
