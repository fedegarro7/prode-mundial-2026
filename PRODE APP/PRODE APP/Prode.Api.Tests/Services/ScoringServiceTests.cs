using Xunit;
using Prode.Api.Entities;
using Prode.Api.Services;

namespace Prode.Api.Tests.Services;

public class ScoringServiceTests
{
    private readonly ScoringService _sut = new();

    [Theory]
    [InlineData(2, 1, 2, 1, 3)]   // Exact score → 3 points
    [InlineData(0, 0, 0, 0, 3)]   // Exact 0-0 → 3 points
    [InlineData(3, 3, 3, 3, 3)]   // Exact draw → 3 points
    public void CalculatePoints_ExactScore_Returns3(
        int predHome, int predAway, int realHome, int realAway, int expected)
    {
        var prediction = new Prediction
        {
            HomeScorePrediction = predHome,
            AwayScorePrediction = predAway
        };
        var match = new Match
        {
            HomeScore = realHome,
            AwayScore = realAway
        };

        var result = _sut.CalculatePoints(prediction, match);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, 0, 3, 0, 1)]   // Home win correct, wrong goals → 1
    [InlineData(0, 2, 0, 4, 1)]   // Away win correct, wrong goals → 1
    [InlineData(1, 1, 2, 2, 1)]   // Draw correct, wrong goals → 1
    [InlineData(3, 1, 2, 0, 1)]   // Home win correct, diff margin → 1
    public void CalculatePoints_CorrectResult_Returns1(
        int predHome, int predAway, int realHome, int realAway, int expected)
    {
        var prediction = new Prediction
        {
            HomeScorePrediction = predHome,
            AwayScorePrediction = predAway
        };
        var match = new Match
        {
            HomeScore = realHome,
            AwayScore = realAway
        };

        var result = _sut.CalculatePoints(prediction, match);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2, 0, 0, 1, 0)]   // Predicted home win, away won → 0
    [InlineData(0, 1, 1, 0, 0)]   // Predicted away win, home won → 0
    [InlineData(1, 1, 3, 0, 0)]   // Predicted draw, home won → 0
    [InlineData(2, 1, 0, 0, 0)]   // Predicted home win, draw happened → 0
    public void CalculatePoints_WrongResult_Returns0(
        int predHome, int predAway, int realHome, int realAway, int expected)
    {
        var prediction = new Prediction
        {
            HomeScorePrediction = predHome,
            AwayScorePrediction = predAway
        };
        var match = new Match
        {
            HomeScore = realHome,
            AwayScore = realAway
        };

        var result = _sut.CalculatePoints(prediction, match);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculatePoints_MatchNoScore_Returns0()
    {
        var prediction = new Prediction
        {
            HomeScorePrediction = 1,
            AwayScorePrediction = 0
        };
        var match = new Match
        {
            HomeScore = null,
            AwayScore = null
        };

        var result = _sut.CalculatePoints(prediction, match);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculatePoints_PartialScore_Returns0()
    {
        var prediction = new Prediction
        {
            HomeScorePrediction = 1,
            AwayScorePrediction = 0
        };
        var match = new Match
        {
            HomeScore = 2,
            AwayScore = null
        };

        var result = _sut.CalculatePoints(prediction, match);

        Assert.Equal(0, result);
    }
}
