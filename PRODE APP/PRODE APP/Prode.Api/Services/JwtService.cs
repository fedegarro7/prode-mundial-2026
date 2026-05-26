using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Prode.Api.Entities;

namespace Prode.Api.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings =
            _configuration.GetSection("Jwt");

        var key =
            jwtSettings["Key"]!;

        var issuer =
            jwtSettings["Issuer"]!;

        var audience =
            jwtSettings["Audience"]!;

        var expireMinutes =
            int.Parse(jwtSettings["ExpireMinutes"]!);

        var claims = new[]
{
    new Claim(
        ClaimTypes.NameIdentifier,
        user.Id.ToString()
    ),

    new Claim(
        ClaimTypes.Email,
        user.Email
    ),

    new Claim(
        ClaimTypes.Name,
        user.Name
    ),

    new Claim(
        ClaimTypes.Role,
        user.IsAdmin ? "Admin" : "User"
    )
};

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key)
            );

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

        var token =
            new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires:
                    DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}