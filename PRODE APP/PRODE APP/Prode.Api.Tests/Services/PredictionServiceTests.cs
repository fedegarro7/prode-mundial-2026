using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.Services;
using Prode.Api.DTOs;
using Prode.Api.Entities;
using System.Security.Claims;

namespace Prode.Api.Tests.Services;

public class PredictionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PredictionService _predictionService;

    public PredictionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _predictionService = new PredictionService(_context);
    }

    [Fact]
    public async Task CreatePrediction_WithValidMatch_ReturnSuccess()
    {
        // Arrange
        var homeTeam = new Team { Id = 1, Name = "Team A" };
        var awayTeam = new Team { Id = 2, Name = "Team B" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash"
        };

        var match = new Match
        {
            Id = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            MatchDate = DateTime.UtcNow.AddDays(1),
            PredictionsLocked = false,
            HomeTeam = homeTeam,
            AwayTeam = awayTeam
        };

        _context.Teams.Add(homeTeam);
        _context.Teams.Add(awayTeam);
        _context.Users.Add(user);
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
                "test"
            )
        );

        var dto = new CreatePredictionDto
        {
            MatchId = 1,
            HomeScorePrediction = 2,
            AwayScorePrediction = 1
        };

        // Act
        var result = await _predictionService.CreatePrediction(dto, claims);

        // Assert
        Assert.Equal("Predicción guardada correctamente", result);
        
        var savedPrediction = _context.Predictions
            .FirstOrDefault(p => p.UserId == user.Id && p.MatchId == 1);
        
        Assert.NotNull(savedPrediction);
        Assert.Equal(2, savedPrediction.HomeScorePrediction);
        Assert.Equal(1, savedPrediction.AwayScorePrediction);
    }

    [Fact]
    public async Task CreatePrediction_WithNonExistentMatch_ThrowsException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
                "test"
            )
        );

        var dto = new CreatePredictionDto
        {
            MatchId = 999,
            HomeScorePrediction = 1,
            AwayScorePrediction = 0
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _predictionService.CreatePrediction(dto, claims)
        );
        Assert.Equal("Partido no encontrado", exception.Message);
    }

    [Fact]
    public async Task CreatePrediction_WithLockedMatch_ReturnLockedMessage()
    {
        // Arrange
        var homeTeam = new Team { Id = 1, Name = "Team A" };
        var awayTeam = new Team { Id = 2, Name = "Team B" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash"
        };

        var match = new Match
        {
            Id = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            MatchDate = DateTime.UtcNow.AddDays(1),
            PredictionsLocked = true,
            HomeTeam = homeTeam,
            AwayTeam = awayTeam
        };

        _context.Teams.Add(homeTeam);
        _context.Teams.Add(awayTeam);
        _context.Users.Add(user);
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
                "test"
            )
        );

        var dto = new CreatePredictionDto
        {
            MatchId = 1,
            HomeScorePrediction = 1,
            AwayScorePrediction = 0
        };

        // Act
        var result = await _predictionService.CreatePrediction(dto, claims);

        // Assert
        Assert.Equal("Predicciones cerradas", result);
    }

    [Fact]
    public async Task CreatePrediction_WithPastMatch_ReturnLockedMessage()
    {
        // Arrange
        var homeTeam = new Team { Id = 1, Name = "Team A" };
        var awayTeam = new Team { Id = 2, Name = "Team B" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash"
        };

        var match = new Match
        {
            Id = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            MatchDate = DateTime.UtcNow.AddDays(-1),
            PredictionsLocked = false,
            HomeTeam = homeTeam,
            AwayTeam = awayTeam
        };

        _context.Teams.Add(homeTeam);
        _context.Teams.Add(awayTeam);
        _context.Users.Add(user);
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
                "test"
            )
        );

        var dto = new CreatePredictionDto
        {
            MatchId = 1,
            HomeScorePrediction = 1,
            AwayScorePrediction = 0
        };

        // Act
        var result = await _predictionService.CreatePrediction(dto, claims);

        // Assert
        Assert.Equal("Las predicciones están cerradas", result);
    }

    [Fact]
    public async Task CreatePrediction_WithUndefinedTeams_ReturnPendingMessage()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash"
        };

        var match = new Match
        {
            Id = 1,
            HomeTeamId = null,
            AwayTeamId = null,
            MatchDate = DateTime.UtcNow.AddDays(1),
            PredictionsLocked = false
        };

        _context.Users.Add(user);
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
                "test"
            )
        );

        var dto = new CreatePredictionDto
        {
            MatchId = 1,
            HomeScorePrediction = 1,
            AwayScorePrediction = 0
        };

        // Act
        var result = await _predictionService.CreatePrediction(dto, claims);

        // Assert
        Assert.Equal("Partido pendiente de definicion", result);
    }

    [Fact]
    public async Task CreatePrediction_UpdateExistingPrediction_Success()
    {
        // Arrange
        var homeTeam = new Team { Id = 1, Name = "Team A" };
        var awayTeam = new Team { Id = 2, Name = "Team B" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash"
        };

        var match = new Match
        {
            Id = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            MatchDate = DateTime.UtcNow.AddDays(1),
            PredictionsLocked = false,
            HomeTeam = homeTeam,
            AwayTeam = awayTeam
        };

        var existingPrediction = new Prediction
        {
            UserId = user.Id,
            MatchId = 1,
            HomeScorePrediction = 1,
            AwayScorePrediction = 0
        };

        _context.Teams.Add(homeTeam);
        _context.Teams.Add(awayTeam);
        _context.Users.Add(user);
        _context.Matches.Add(match);
        _context.Predictions.Add(existingPrediction);
        await _context.SaveChangesAsync();

        var claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
                "test"
            )
        );

        var dto = new CreatePredictionDto
        {
            MatchId = 1,
            HomeScorePrediction = 3,
            AwayScorePrediction = 2
        };

        // Act
        var result = await _predictionService.CreatePrediction(dto, claims);

        // Assert
        Assert.Equal("Predicción guardada correctamente", result);
        
        var updatedPrediction = _context.Predictions
            .FirstOrDefault(p => p.UserId == user.Id && p.MatchId == 1);
        
        Assert.NotNull(updatedPrediction);
        Assert.Equal(3, updatedPrediction.HomeScorePrediction);
        Assert.Equal(2, updatedPrediction.AwayScorePrediction);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
