namespace Prode.Api.DTOs;

/// <summary>
/// A single news item fetched from an external RSS feed.
/// </summary>
public record NewsItemDto(
    string Title,
    string Description,
    string Link,
    string Source,
    string SourceUrl,
    string SourceColor,
    DateTime PublishedAt,
    string? ImageUrl
);
