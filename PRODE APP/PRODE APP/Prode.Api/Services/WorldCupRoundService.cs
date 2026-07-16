using Prode.Api.Entities;

namespace Prode.Api.Services;

public static class WorldCupRoundService
{
    public const string GroupStage = "GROUP_STAGE";
    public const string RoundOf32 = "ROUND_OF_32";
    public const string RoundOf16 = "ROUND_OF_16";
    public const string QuarterFinals = "QUARTER_FINALS";
    public const string SemiFinals = "SEMI_FINALS";
    public const string FinalRound = "FINAL_ROUND";

    private static readonly string[] KnockoutRoundKeys =
    [
        RoundOf32,
        RoundOf16,
        QuarterFinals,
        SemiFinals,
        FinalRound
    ];

    public static string GetRoundKey(Match match)
    {
        if (!string.IsNullOrWhiteSpace(match.GroupName))
        {
            return GroupStage;
        }

        var stage = Normalize(match.Stage);

        if (stage.Contains("ROUND OF 32") || stage.Contains("DIECISEIS"))
        {
            return RoundOf32;
        }

        if (stage.Contains("ROUND OF 16") || stage.Contains("OCTAV"))
        {
            return RoundOf16;
        }

        if (stage.Contains("QUARTER") || stage.Contains("CUART"))
        {
            return QuarterFinals;
        }

        if (stage.Contains("SEMI"))
        {
            return SemiFinals;
        }

        if (
            stage.Contains("THIRD") ||
            stage.Contains("TERCER") ||
            stage.Contains("PLAY-OFF") ||
            stage.Contains("BRONZE") ||
            stage == "FINAL" ||
            stage.Contains("GRAN FINAL")
        )
        {
            return FinalRound;
        }

        return string.Empty;
    }

    public static bool IsKnockoutRound(string roundKey) =>
        KnockoutRoundKeys.Contains(roundKey);

    public static IReadOnlyCollection<string> GetKnockoutRoundKeys() =>
        KnockoutRoundKeys;

    public static int GetExactScoreBasePoints(Match match)
    {
        if (!string.IsNullOrWhiteSpace(match.GroupName))
        {
            return 3;
        }

        var roundKey = GetRoundKey(match);

        return roundKey switch
        {
            RoundOf32 => 4,
            RoundOf16 => 5,
            QuarterFinals => 7,
            SemiFinals => 10,
            FinalRound => IsFinal(match) ? 15 : 12,
            _ => 3
        };
    }

    public static bool IsFinal(Match match)
    {
        var stage = Normalize(match.Stage);

        return stage == "FINAL" || stage.Contains("GRAN FINAL");
    }

    public static bool IsRoundStartLocked(IEnumerable<Match> matches, DateTime now)
    {
        var firstMatch = matches
            .OrderBy(m => m.MatchDate)
            .FirstOrDefault();

        return firstMatch != null &&
            (firstMatch.PredictionsLocked || firstMatch.MatchDate <= now);
    }

    private static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
