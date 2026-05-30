namespace InfoTrack.Core.Models;

public sealed record LocationResult
{
    public required string Location { get; init; }
    public required IReadOnlyList<Solicitor> Solicitors { get; init; }
    public bool FromCache { get; init; }
    public DateTimeOffset CachedAt { get; init; }
    public int NewlyDiscoveredCount { get; init; }
    public string? ErrorMessage { get; init; }
}