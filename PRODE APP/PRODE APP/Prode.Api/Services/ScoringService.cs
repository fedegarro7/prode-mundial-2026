using Prode.Api.Entities;

namespace Prode.Api.Services;

public class ScoringService
{
    public int CalculatePoints(
        Prediction prediction,
        Match match
    )
    {
        if (
            !match.IsFinished ||
            match.HomeScore == null ||
            match.AwayScore == null
        )
        {
            return 0;
        }

        var exactScore =
            prediction.HomeScorePrediction ==
                match.HomeScore
            &&
            prediction.AwayScorePrediction ==
                match.AwayScore;

        if (exactScore)
        {
            return 3;
        }

        var predictedResult =
            GetResult(
                prediction.HomeScorePrediction,
                prediction.AwayScorePrediction
            );

        var actualResult =
            GetResult(
                match.HomeScore.Value,
                match.AwayScore.Value
            );

        if (predictedResult == actualResult)
        {
            return 1;
        }

        return 0;
    }

    private string GetResult(
        int homeScore,
        int awayScore
    )
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
