using System.IdentityModel.Tokens.Jwt;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Prode.Api.Services;
using Prode.Api.Entities;
using System.Security.Claims;

namespace Prode.Api.Tests.Services;

public class JwtServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IConfigurationSection> _mockJwtSection;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockJwtSection = new Mock<IConfigurationSection>();

        _mockJwtSection["Key"] = "this-is-a-very-long-secret-key-for-testing-purposes-at-least-32-characters";
        _mockJwtSection["Issuer"] = "ProdeApp";
        _mockJwtSection["Audience"] = "ProdeUsers";
        _mockJwtSection["ExpireMinutes"] = "60";

        _mockConfiguration
            .Setup(c => c.GetSection("Jwt"))
            .Returns(_mockJwtSection.Object);

        _jwtService = new JwtService(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            IsAdmin = false,
            PasswordHash = "hash"
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_WithAdminUser_IncludesAdminRole()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Name = "Admin User",
            IsAdmin = true,
            PasswordHash = "hash"
        };

        // Act
        var token = _jwtService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        Assert.NotNull(jwtToken);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("Admin", roleClaim.Value);
    }

    [Fact]
    public void GenerateToken_WithRegularUser_IncludesUserRole()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "Regular User",
            IsAdmin = false,
            PasswordHash = "hash"
        };

        // Act
        var token = _jwtService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        Assert.NotNull(jwtToken);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("User", roleClaim.Value);
    }

    [Fact]
    public void GenerateToken_IncludesUserClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "claim@example.com";
        var name = "Claim Test";
        var user = new User
        {
            Id = userId,
            Email = email,
            Name = name,
            IsAdmin = false,
            PasswordHash = "hash"
        };

        // Act
        var token = _jwtService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        Assert.NotNull(jwtToken);
        var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

        Assert.NotNull(idClaim);
        Assert.Equal(userId.ToString(), idClaim.Value);
        Assert.NotNull(emailClaim);
        Assert.Equal(email, emailClaim.Value);
        Assert.NotNull(nameClaim);
        Assert.Equal(name, nameClaim.Value);
    }

    [Fact]
    public void GenerateToken_TokenExpiresInConfiguredMinutes()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "expire@example.com",
            Name = "Expire Test",
            IsAdmin = false,
            PasswordHash = "hash"
        };

        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _jwtService.GenerateToken(user);
        var afterGeneration = DateTime.UtcNow;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        Assert.NotNull(jwtToken);
        Assert.NotNull(jwtToken.ValidTo);
        
        // Token should expire in approximately 60 minutes
        var expectedExpiry = beforeGeneration.AddMinutes(60);
        var timeDifference = Math.Abs((jwtToken.ValidTo - expectedExpiry).TotalSeconds);
        
        Assert.True(timeDifference < 5, $"Token expiry differs from expected by {timeDifference} seconds");
    }
}
