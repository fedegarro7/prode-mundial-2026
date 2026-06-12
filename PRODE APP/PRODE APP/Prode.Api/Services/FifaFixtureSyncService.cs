using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prode.Api.Data;
using Prode.Api.Entities;

namespace Prode.Api.Services;

public class FifaFixtureSyncService
{
    private const int FifaMatchStatusFinished = 0;

    private const string WorldCup2026MatchesUrl =
        "https://api.fifa.com/api/v3/calendar/matches?language=en&count=200&idCompetition=17&idSeason=285023";

    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FifaFixtureSyncService> _logger;

    public FifaFixtureSyncService(
        AppDbContext context,
        HttpClient httpClient,
        ILogger<FifaFixtureSyncService> logger
    )
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<FifaFixtureSyncResult> SyncWorldCup2026Async(
        CancellationToken cancellationToken = default
    )
    {
        using var stream = await _httpClient.GetStreamAsync(
            WorldCup2026MatchesUrl,
            cancellationToken
        );

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken
        );

        var root = document.RootElement;

        if (
            !root.TryGetProperty("Results", out var results) ||
            results.ValueKind != JsonValueKind.Array
        )
        {
            throw new InvalidOperationException(
                "La respuesta de FIFA no contiene resultados de partidos"
            );
        }

        var teams = new Dictionary<string, FifaTeamData>();
        var stadiums = new Dictionary<string, FifaStadiumData>();
        var matches = new List<FifaMatchData>();

        foreach (var match in results.EnumerateArray())
        {
            var groupName = GetLocalizedDescription(
                match,
                "GroupName"
            );

            var homeTeam = ParseTeam(match, "Home", groupName);
            var awayTeam = ParseTeam(match, "Away", groupName);
            var stadium = ParseStadium(match);

            if (homeTeam != null)
            {
                teams[homeTeam.FifaId] = homeTeam;
            }

            if (awayTeam != null)
            {
                teams[awayTeam.FifaId] = awayTeam;
            }

            stadiums[stadium.FifaId] = stadium;

            matches.Add(ParseMatch(
                match,
                homeTeam?.FifaId,
                awayTeam?.FifaId,
                stadium.FifaId,
                groupName
            ));
        }

        await UpsertTeamsAsync(teams.Values, cancellationToken);
        await UpsertStadiumsAsync(stadiums.Values, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await UpsertMatchesAsync(matches, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Synced {MatchCount} FIFA World Cup 2026 matches from {SourceUrl}",
            matches.Count,
            WorldCup2026MatchesUrl
        );

        return new FifaFixtureSyncResult
        {
            SourceUrl = WorldCup2026MatchesUrl,
            Teams = teams.Count,
            Stadiums = stadiums.Count,
            Matches = matches.Count,
            SyncedAtUtc = DateTime.UtcNow
        };
    }

    private async Task UpsertTeamsAsync(
        IEnumerable<FifaTeamData> teamData,
        CancellationToken cancellationToken
    )
    {
        var existingByFifaId = await _context.Teams
            .Where(x => x.FifaId != null)
            .ToDictionaryAsync(x => x.FifaId!, cancellationToken);

        var existingByCode = await _context.Teams
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var data in teamData)
        {
            if (
                !existingByFifaId.TryGetValue(data.FifaId, out var team) &&
                !existingByCode.TryGetValue(data.Code, out team)
            )
            {
                team = new Team();
                _context.Teams.Add(team);
            }

            team.FifaId = data.FifaId;
            team.Name = data.Name;
            team.Code = data.Code;
            team.FlagUrl = data.FlagUrl;
            team.Group = data.GroupName;
        }
    }

    private async Task UpsertStadiumsAsync(
        IEnumerable<FifaStadiumData> stadiumData,
        CancellationToken cancellationToken
    )
    {
        var existingByFifaId = await _context.Stadiums
            .Where(x => x.FifaId != null)
            .ToDictionaryAsync(x => x.FifaId!, cancellationToken);

        foreach (var data in stadiumData)
        {
            if (!existingByFifaId.TryGetValue(data.FifaId, out var stadium))
            {
                stadium = new Stadium();
                _context.Stadiums.Add(stadium);
            }

            stadium.FifaId = data.FifaId;
            stadium.Name = data.Name;
            stadium.City = data.City;
            stadium.Country = data.Country;
        }
    }

    private async Task UpsertMatchesAsync(
        IEnumerable<FifaMatchData> matchData,
        CancellationToken cancellationToken
    )
    {
        var teamsByFifaId = await _context.Teams
            .Where(x => x.FifaId != null)
            .ToDictionaryAsync(x => x.FifaId!, cancellationToken);

        var stadiumsByFifaId = await _context.Stadiums
            .Where(x => x.FifaId != null)
            .ToDictionaryAsync(x => x.FifaId!, cancellationToken);

        var matchesByFifaId = await _context.Matches
            .Where(x => x.FifaId != null)
            .Include(x => x.Predictions)
            .ToDictionaryAsync(x => x.FifaId!, cancellationToken);

        var scoring = new ScoringService();
        var toRescore = new List<Match>();

        foreach (var data in matchData)
        {
            if (!matchesByFifaId.TryGetValue(data.FifaId, out var match))
            {
                match = new Match();
                _context.Matches.Add(match);
            }

            int? homeTeamId = data.HomeTeamFifaId == null
                ? null
                : teamsByFifaId[data.HomeTeamFifaId].Id;

            int? awayTeamId = data.AwayTeamFifaId == null
                ? null
                : teamsByFifaId[data.AwayTeamFifaId].Id;

            match.FifaId = data.FifaId;
            match.MatchNumber = data.MatchNumber;
            match.HomeTeamId = homeTeamId;
            match.HomePlaceholder = data.HomePlaceholder;
            match.AwayTeamId = awayTeamId;
            match.AwayPlaceholder = data.AwayPlaceholder;
            match.MatchDate = data.MatchDate;
            match.Stage = data.Stage;
            match.GroupName = data.GroupName;
            match.StadiumId = stadiumsByFifaId[data.StadiumFifaId].Id;

            var isFinalScore =
                data.IsFinished &&
                data.HomeScore.HasValue &&
                data.AwayScore.HasValue;

            var wasFinished = match.IsFinished;
            var previousHomeScore = match.HomeScore;
            var previousAwayScore = match.AwayScore;

            if (data.HomeScore.HasValue && data.AwayScore.HasValue)
            {
                match.HomeScore = data.HomeScore;
                match.AwayScore = data.AwayScore;
            }
            else if (!data.IsFinished)
            {
                match.HomeScore = null;
                match.AwayScore = null;
            }

            match.IsFinished = isFinalScore;

            if (isFinalScore)
            {
                var scoreChanged =
                    !wasFinished ||
                    previousHomeScore != data.HomeScore ||
                    previousAwayScore != data.AwayScore;

                if (scoreChanged && match.Predictions.Count > 0)
                    toRescore.Add(match);
            }
            else if (wasFinished && match.Predictions.Count > 0)
            {
                foreach (var prediction in match.Predictions)
                {
                    prediction.PointsEarned = 0;
                }
            }

            match.PredictionsLocked =
                match.IsFinished ||
                data.MatchDate <= DateTime.UtcNow ||
                !homeTeamId.HasValue ||
                !awayTeamId.HasValue;
        }

        // Recalculate PointsEarned for every prediction of newly-scored matches.
        foreach (var match in toRescore)
        {
            foreach (var prediction in match.Predictions)
            {
                prediction.PointsEarned =
                    scoring.CalculatePoints(prediction, match);
            }
        }
    }

    private static FifaTeamData? ParseTeam(
        JsonElement match,
        string propertyName,
        string groupName
    )
    {
        if (
            !match.TryGetProperty(propertyName, out var team) ||
            team.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
        )
        {
            return null;
        }

        var fifaId = GetString(team, "IdTeam");
        var code = GetString(team, "Abbreviation");

        if (string.IsNullOrWhiteSpace(fifaId) || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return new FifaTeamData(
            fifaId,
            GetLocalizedDescription(team, "TeamName", code),
            code,
            BuildFlagUrl(team),
            groupName
        );
    }

    private static FifaStadiumData ParseStadium(JsonElement match)
    {
        var stadium = match.GetProperty("Stadium");
        var fifaId = GetString(stadium, "IdStadium");

        if (string.IsNullOrWhiteSpace(fifaId))
        {
            throw new InvalidOperationException(
                "Un partido de FIFA no contiene IdStadium"
            );
        }

        return new FifaStadiumData(
            fifaId,
            GetLocalizedDescription(stadium, "Name", fifaId),
            GetLocalizedDescription(stadium, "CityName"),
            GetString(stadium, "IdCountry") ?? string.Empty
        );
    }

    private static FifaMatchData ParseMatch(
        JsonElement match,
        string? homeTeamFifaId,
        string? awayTeamFifaId,
        string stadiumFifaId,
        string groupName
    )
    {
        var fifaId = GetString(match, "IdMatch");
        var date = GetString(match, "Date");

        if (string.IsNullOrWhiteSpace(fifaId) || string.IsNullOrWhiteSpace(date))
        {
            throw new InvalidOperationException(
                "Un partido de FIFA no contiene IdMatch o Date"
            );
        }

        return new FifaMatchData(
            fifaId,
            GetInt32(match, "MatchNumber"),
            homeTeamFifaId,
            GetString(match, "PlaceHolderA") ?? string.Empty,
            awayTeamFifaId,
            GetString(match, "PlaceHolderB") ?? string.Empty,
            DateTime.Parse(
                date,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal
            ),
            GetLocalizedDescription(match, "StageName"),
            groupName,
            stadiumFifaId,
            IsFifaFinal(match),
            GetInt32(match, "HomeTeamScore"),
            GetInt32(match, "AwayTeamScore")
        );
    }

    private static bool IsFifaFinal(JsonElement match)
    {
        return GetInt32(match, "MatchStatus") == FifaMatchStatusFinished;
    }

    private static string BuildFlagUrl(JsonElement team)
    {
        var pictureUrl = GetString(team, "PictureUrl");

        if (string.IsNullOrWhiteSpace(pictureUrl))
        {
            return string.Empty;
        }

        return pictureUrl
            .Replace("{format}", "sq", StringComparison.Ordinal)
            .Replace("{size}", "4", StringComparison.Ordinal);
    }

    private static string GetLocalizedDescription(
        JsonElement parent,
        string propertyName,
        string fallback = ""
    )
    {
        if (
            !parent.TryGetProperty(propertyName, out var values) ||
            values.ValueKind != JsonValueKind.Array
        )
        {
            return fallback;
        }

        foreach (var value in values.EnumerateArray())
        {
            if (
                string.Equals(
                    GetString(value, "Locale"),
                    "en-GB",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetString(value, "Description") ?? fallback;
            }
        }

        return values.EnumerateArray()
            .Select(x => GetString(x, "Description"))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ??
            fallback;
    }

    private static string? GetString(
        JsonElement element,
        string propertyName
    )
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static int? GetInt32(
        JsonElement element,
        string propertyName
    )
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out var value)
        )
        {
            return value;
        }

        return null;
    }

    private sealed record FifaTeamData(
        string FifaId,
        string Name,
        string Code,
        string FlagUrl,
        string GroupName
    );

    private sealed record FifaStadiumData(
        string FifaId,
        string Name,
        string City,
        string Country
    );

    private sealed record FifaMatchData(
        string FifaId,
        int? MatchNumber,
        string? HomeTeamFifaId,
        string HomePlaceholder,
        string? AwayTeamFifaId,
        string AwayPlaceholder,
        DateTime MatchDate,
        string Stage,
        string GroupName,
        string StadiumFifaId,
        bool IsFinished,
        int? HomeScore,
        int? AwayScore
    );
}

public class FifaFixtureSyncResult
{
    public string SourceUrl { get; set; } = string.Empty;

    public int Teams { get; set; }

    public int Stadiums { get; set; }

    public int Matches { get; set; }

    public DateTime SyncedAtUtc { get; set; }
}
