using Prode.Api.Entities;

namespace Prode.Api.Services;

public class ScoringService
{
    public int CalculatePoints(
        Prediction prediction,
        Match match
    ) => CalculatePredictionScore(prediction, match).TotalPoints;

    public PredictionScore CalculatePredictionScore(
        Prediction prediction,
        Match match,
        ScoringContext? context = null
    )
    {
        context ??= ScoringContext.Empty;

        if (
            !match.IsFinished ||
            match.HomeScore == null ||
            match.AwayScore == null
        )
        {
            return PredictionScore.Zero;
        }

        var exactScore =
            prediction.HomeScorePrediction == match.HomeScore &&
            prediction.AwayScorePrediction == match.AwayScore;

        var predictedResult = GetResult(
            prediction.HomeScorePrediction,
            prediction.AwayScorePrediction
        );

        var actualResult = GetResult(
            match.HomeScore.Value,
            match.AwayScore.Value
        );

        var correctResult = predictedResult == actualResult;

        if (!exactScore && !correctResult)
        {
            return PredictionScore.Zero;
        }

        var basePoints = exactScore
            ? WorldCupRoundService.GetExactScoreBasePoints(match)
            : 1;

        var multiplierBonus = 0;

        if (exactScore)
        {
            // Each multiplier is calculated independently from the base and their bonuses add up.
            // Partido Bomba adds 1× base, Gol de Oro adds 2× base. Neither compounds the other.
            if (context.IsBombMatch)
                multiplierBonus += basePoints;          // ×2 total
            if (context.HasGoldenGoal)
                multiplierBonus += basePoints * 2;      // ×3 total (or ×4 when combined)
        }

        var multiplierPoints = basePoints + multiplierBonus;

        var captainBonus = context.HasCaptainTeam && correctResult ? 5 : 0;
        var total = multiplierPoints + captainBonus;

        return new PredictionScore(
            basePoints,
            multiplierBonus,
            captainBonus,
            total,
            exactScore,
            correctResult
        );
    }

    private static string GetResult(int homeScore, int awayScore)
    {
        if (homeScore > awayScore)
        {
            return "HOME";
        }

        if (awayScore > homeScore)
        {
            return "AWAY";
        }

        return "DRAW";
    }
}

public sealed record ScoringContext(
    bool HasGoldenGoal,
    bool IsBombMatch,
    bool HasCaptainTeam
)
{
    public static ScoringContext Empty { get; } = new(false, false, false);
}

public sealed record PredictionScore(
    int BasePoints,
    int MultiplierBonusPoints,
    int CaptainBonusPoints,
    int TotalPoints,
    bool IsExactScore,
    bool IsCorrectResult
)
{
    public static PredictionScore Zero { get; } = new(0, 0, 0, 0, false, false);
}

