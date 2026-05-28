using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Api.DTOs;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Prode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private static readonly (string Name, string FeedUrl, string SiteUrl, string Color)[] Sources =
    [
        ("Olé",              "https://www.ole.com.ar/rss/seleccion/",                "https://www.ole.com.ar",          "#E8000D"),
        ("Infobae",          "https://www.infobae.com/arc/outboundfeeds/rss/category/deportes/", "https://www.infobae.com", "#E40000"),
        ("La Nación",        "https://www.lanacion.com.ar/arcio/rss/category/deportes/", "https://www.lanacion.com.ar", "#2A6496"),
        ("AS",               "https://feeds.as.com/mrss-s/pages/as/site/as.com/section/futbol/portada", "https://as.com", "#D0021B"),
        ("Mundo Deportivo",  "https://www.mundodeportivo.com/rss/futbol",            "https://www.mundodeportivo.com",  "#0097D6"),
    ];

    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static List<NewsItemDto>? _cache;
    private static DateTime _cacheExpiry = DateTime.MinValue;

    private readonly ILogger<NewsController> _logger;

    public NewsController(ILogger<NewsController> logger) => _logger = logger;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetNews()
    {
        if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
            return Ok(_cache);

        await Lock.WaitAsync();
        try
        {
            if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
                return Ok(_cache);

            var tasks = Sources.Select(s => FetchFeedAsync(s.Name, s.FeedUrl, s.SiteUrl, s.Color));
            var results = await Task.WhenAll(tasks);

            _cache = results
                .SelectMany(x => x)
                .OrderByDescending(n => n.PublishedAt)
                .Take(30)
                .ToList();

            _logger.LogInformation("News refreshed: {Count} items [{Sources}]",
                _cache.Count,
                string.Join(", ", results.Select((r, i) => $"{Sources[i].Name}={r.Count()}")));

            _cacheExpiry = DateTime.UtcNow.AddMinutes(3);
            return Ok(_cache);
        }
        finally { Lock.Release(); }
    }

    [HttpGet("debug")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugFeeds()
    {
        var results = new List<object>();
        foreach (var (name, url, siteUrl, color) in Sources)
        {
            try
            {
                var xml = await DownloadFeed(url);
                var doc = XDocument.Parse(xml);
                var allItems = doc.Descendants("item").ToList();
                var sample = allItems.Take(5).Select(i => i.Element("title")?.Value ?? "").ToList();
                results.Add(new { Source = name, Ok = true, Total = allItems.Count, Titles = sample });
            }
            catch (Exception ex)
            {
                results.Add(new { Source = name, Ok = false, Error = ex.Message });
            }
        }
        return Ok(results);
    }

    // ── Download with compression + realistic headers ─────────────────────────
    private static async Task<string> DownloadFeed(string url)
    {
        using var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept", "application/rss+xml, application/xml, text/xml, text/html, */*");
        client.DefaultRequestHeaders.Add("Accept-Language", "es-AR,es;q=0.9");

        var raw = await client.GetStringAsync(url);
        return SanitizeXml(raw);
    }

    // ── Sanitize HTML entities that break XDocument.Parse ──────────────────────
    private static string SanitizeXml(string xml)
    {
        // Remove BOM if present
        if (xml.Length > 0 && xml[0] == '\uFEFF') xml = xml[1..];

        // Replace common HTML named entities with numeric equivalents
        xml = Regex.Replace(xml, @"&(aacute|eacute|iacute|oacute|uacute|ntilde|Aacute|Eacute|Iacute|Oacute|Uacute|Ntilde|uuml|Uuml|iquest|iexcl|nbsp|mdash|ndash|laquo|raquo|ldquo|rdquo|lsquo|rsquo|hellip|copy|reg|trade|euro|pound|yen|cent|deg|middot|bull|times|divide|plusmn|frac12|frac14|frac34);",
            m => m.Groups[1].Value switch
            {
                "aacute" => "&#225;", "eacute" => "&#233;", "iacute" => "&#237;",
                "oacute" => "&#243;", "uacute" => "&#250;", "ntilde" => "&#241;",
                "Aacute" => "&#193;", "Eacute" => "&#201;", "Iacute" => "&#205;",
                "Oacute" => "&#211;", "Uacute" => "&#218;", "Ntilde" => "&#209;",
                "uuml" => "&#252;", "Uuml" => "&#220;",
                "iquest" => "&#191;", "iexcl" => "&#161;",
                "nbsp" => "&#160;", "mdash" => "&#8212;", "ndash" => "&#8211;",
                "laquo" => "&#171;", "raquo" => "&#187;",
                "ldquo" => "&#8220;", "rdquo" => "&#8221;",
                "lsquo" => "&#8216;", "rsquo" => "&#8217;",
                "hellip" => "&#8230;", "copy" => "&#169;",
                "reg" => "&#174;", "trade" => "&#8482;",
                "euro" => "&#8364;", "pound" => "&#163;",
                "yen" => "&#165;", "cent" => "&#162;",
                "deg" => "&#176;", "middot" => "&#183;",
                "bull" => "&#8226;", "times" => "&#215;",
                "divide" => "&#247;", "plusmn" => "&#177;",
                "frac12" => "&#189;", "frac14" => "&#188;", "frac34" => "&#190;",
                _ => m.Value
            });

        return xml;
    }

    private async Task<IEnumerable<NewsItemDto>> FetchFeedAsync(
        string name, string feedUrl, string siteUrl, string color)
    {
        try
        {
            var xml = await DownloadFeed(feedUrl);
            var items = ParseRss(xml, name, siteUrl, color).ToList();
            _logger.LogInformation("Feed {Source}: {Count} items", name, items.Count);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Feed {Source} failed: {Error}", name, ex.Message);
            return [];
        }
    }

    private static IEnumerable<NewsItemDto> ParseRss(
        string xml, string sourceName, string sourceUrl, string sourceColor)
    {
        XNamespace media = "http://search.yahoo.com/mrss/";
        XNamespace content = "http://purl.org/rss/1.0/modules/content/";
        var doc = XDocument.Parse(xml);

        return doc.Descendants("item")
            .Select(item =>
            {
                var title = item.Element("title")?.Value?.Trim() ?? "";

                var link = (item.Element("link")?.Value?.Trim()
                         ?? item.Elements()
                                .FirstOrDefault(e => e.Name.LocalName == "link")
                                ?.Attribute("href")?.Value
                         ?? "").Trim();

                if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    link = sourceUrl;

                var rawDesc = item.Element("description")?.Value ?? "";
                var desc = StripHtml(rawDesc);
                if (desc.Length > 200) desc = desc[..200] + "\u2026";

                var pubDateStr = item.Element("pubDate")?.Value ?? "";
                DateTime.TryParse(pubDateStr, out var pubDate);
                if (pubDate == default) pubDate = DateTime.UtcNow;

                var image = item.Element("enclosure")?.Attribute("url")?.Value
                         ?? item.Element(media + "content")?.Attribute("url")?.Value
                         ?? item.Element(media + "thumbnail")?.Attribute("url")?.Value
                         ?? item.Elements(media + "content")
                                .FirstOrDefault(e => e.Attribute("medium")?.Value == "image")
                                ?.Attribute("url")?.Value
                         ?? item.Elements()
                                .FirstOrDefault(e => e.Name.LocalName == "thumbnail")
                                ?.Attribute("url")?.Value
                         ?? ExtractImageFromHtml(item.Element("description")?.Value ?? "")
                         ?? ExtractImageFromHtml(item.Element(content + "encoded")?.Value ?? "");

                return new NewsItemDto(title, desc, link, sourceName, sourceUrl, sourceColor, pubDate, image ?? "");
            })
            .Where(n => !string.IsNullOrWhiteSpace(n.Title) && n.Link.StartsWith("http"))
            .Where(n => (DateTime.UtcNow - n.PublishedAt).TotalDays <= 7)
            .Where(n => IsMundialRelated(n.Title, n.Description))
            .Take(2);
    }

    private static readonly Regex ImgSrcRx = new(
        @"<img[^>]+src=[""']([^""']+)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string? ExtractImageFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var match = ImgSrcRx.Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    // ── Title-only keywords (broad terms, only match in title) ────────────────
    private static readonly string[] TitleKeywords =
    [
        "mundial 2026", "world cup 2026", "copa del mundo",
        "mundial", "world cup",
        "eliminatorias", "fase de grupos",
        "usa 2026", "estados unidos 2026",
        "fifa 2026", "convocatoria", "convocados",
        "seleccion argentina", "la seleccion", "seleccion de",
        "concentracion", "amistoso", "amistosos",
    ];

    // ── Any-text keywords (specific, match in title or description) ───────────
    private static readonly string[] AnyKeywords =
    [
        "mundial 2026", "world cup 2026", "copa del mundo 2026",
        "messi", "scaloni", "lautaro", "julian alvarez",
        "di maria", "mbappe", "haaland", "vinicius",
        "neymar", "cristiano ronaldo", "bellingham",
        "albiceleste", "sorteo mundial", "fixture mundial",
        "sede del mundial", "sedes del mundial", "estadio azteca",
        "convocatoria mundial", "lista mundialista",
        "selecciones clasificadas", "grupo a ", "grupo b ",
        "grupo c ", "grupo d ", "grupo e ", "grupo f ",
        "grupo g ", "grupo h ", "grupo i ", "grupo j ",
        "grupo k ", "grupo l ",
    ];

    // ── Exclusions ────────────────────────────────────────────────────────────
    private static readonly string[] Exclusions =
    [
        "mundial de clubes", "club world cup",
        "champions league", "europa league", "conference league",
        "liga profesional", "copa de la liga", "superliga",
        "river plate", "boca juniors",
        "premier league", "serie a", "bundesliga", "ligue 1",
        "traspaso", "mercado de pases",
        "copa libertadores", "copa sudamericana",
        "la liga", "copa del rey",
    ];

    private static bool IsMundialRelated(string title, string desc)
    {
        var titleLow = title.ToLowerInvariant();
        var fullLow = (title + " " + desc).ToLowerInvariant();

        if (Exclusions.Any(k => fullLow.Contains(k))) return false;
        if (TitleKeywords.Any(k => titleLow.Contains(k))) return true;
        if (AnyKeywords.Any(k => fullLow.Contains(k))) return true;
        return false;
    }

    private static readonly Regex HtmlTagRx = new("<[^>]*>", RegexOptions.Compiled);

    private static string StripHtml(string input) =>
        HtmlTagRx.Replace(WebUtility.HtmlDecode(input), " ")
                 .Replace("  ", " ")
                 .Trim();
}
