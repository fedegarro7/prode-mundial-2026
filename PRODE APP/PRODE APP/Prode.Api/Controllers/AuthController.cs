using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public AuthController(
        AppDbContext context,
        JwtService jwtService,
        IWebHostEnvironment environment
    )
    {
        _context = context;
        _jwtService = jwtService;
        _environment = environment;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest("El nombre es obligatorio");
        }

        if (!IsValidPassword(dto.Password))
        {
            return BadRequest("La contrasena debe tener al menos 8 caracteres");
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

        return Ok(new AuthResponseDto
        {
            Token = token,
            Name = user.Name,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        });
    }

    [HttpPost("login")]
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

        return Ok(new AuthResponseDto
        {
            Token = token,
            Name = user.Name,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!IsValidPassword(dto.NewPassword))
        {
            return BadRequest("La nueva contrasena debe tener al menos 8 caracteres");
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
            return BadRequest("La contrasena actual no es correcta");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        user.PasswordResetRequestedAt = null;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        string? token = null;

        if (user is not null)
        {
            token = CreateResetToken();

            user.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword(token);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            user.PasswordResetRequestedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Console.WriteLine($"PASSWORD RESET TOKEN for {email}: {token}");
        }

        return Ok(new ForgotPasswordResponseDto
        {
            Message = "Si el email existe, generamos un token de recuperacion.",
            DevelopmentResetToken = _environment.IsDevelopment() ? token : null
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (!IsValidPassword(dto.NewPassword))
        {
            return BadRequest("La nueva contrasena debe tener al menos 8 caracteres");
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
}
