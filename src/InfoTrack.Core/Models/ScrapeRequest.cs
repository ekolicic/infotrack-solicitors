namespace InfoTrack.Core.Models;

public sealed record ScrapeRequest
{
    public required IReadOnlyList<string> Locations { get; init; }
    public bool ForceRefresh { get; init; }
}
