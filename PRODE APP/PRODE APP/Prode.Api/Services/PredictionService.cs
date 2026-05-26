using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.DTOs;
using Prode.Api.Entities;
using System.Security.Claims;


namespace Prode.Api.Services;

public class PredictionService
{
    private readonly AppDbContext _context;

    public PredictionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreatePrediction(
        CreatePredictionDto dto,
        ClaimsPrincipal userClaims
    )
    {
        var match = await _context.Matches
            .FirstOrDefaultAsync(x => x.Id == dto.MatchId);


        if (match == null)
        {
            throw new Exception("Partido no encontrado");
        }

        if (!match.HomeTeamId.HasValue || !match.AwayTeamId.HasValue)
        {
            return "Partido pendiente de definicion";
        }

        if (
      match.PredictionsLocked
      ||
      match.MatchDate <= DateTime.UtcNow
  )
        {
            return "Predicciones cerradas";
        }

        if (DateTime.UtcNow >= match.MatchDate)
        {
            return "Las predicciones están cerradas";
        }

        var userIdClaim = userClaims.Claims.FirstOrDefault(
     x => x.Type == ClaimTypes.NameIdentifier
 );

        if (userIdClaim == null)
        {
            throw new Exception("Usuario inválido");
        }

        var userId = Guid.Parse(userIdClaim.Value);

        var existingPrediction =
            await _context.Predictions
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.MatchId == dto.MatchId
                );

        if (existingPrediction != null)
        {
            existingPrediction.HomeScorePrediction =
                dto.HomeScorePrediction;

            existingPrediction.AwayScorePrediction =
                dto.AwayScorePrediction;
        }
        else
        {
            var prediction = new Prediction
            {
                UserId = userId,
                MatchId = dto.MatchId,
                HomeScorePrediction =
                    dto.HomeScorePrediction,
                AwayScorePrediction =
                    dto.AwayScorePrediction
            };

            _context.Predictions.Add(prediction);
        }

        await _context.SaveChangesAsync();

        return "Predicción guardada correctamente";
    }
}
