
namespace InfoTrack.Core.Models;

public sealed record Solicitor
{
    public required string Name { get; init; }
    public string? Address { get; init; }
    public string? Telephone { get; init; }
    public string? Website { get; init; }
    public string? Location { get; init; }
    public decimal? StarRating { get; init; }
    public int? ReviewCount { get; init; }
    public bool IsNewlyDiscovered { get; init; }
    public DateTimeOffset ScrapedAt { get; init; } = DateTimeOffset.UtcNow;
}
