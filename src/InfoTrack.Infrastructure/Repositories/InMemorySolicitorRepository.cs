using InfoTrack.Core.Interfaces;
using InfoTrack.Core.Models;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Repositories;

/// <summary>
/// In-memory cache of scrape results, keyed by location (case-insensitive).
/// Tracks which solicitor names are newly seen since the last scrape to surface
/// "new entrant" alerts in the UI.
/// </summary>
public sealed class InMemorySolicitorRepository : ISolicitorRepository
{
    // Cache duration is set to 24 hours, but can be adjusted as needed.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly ISolicitorScraper _scraper;
    private readonly ILogger<InMemorySolicitorRepository> _logger;

    // Cache dictionary mapping location to its cached scrape result and expiration time.
    private readonly Dictionary<string, CacheEntry> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, HashSet<string>> _seenNames =
        new(StringComparer.OrdinalIgnoreCase);


    // Semaphore to ensure that only one scrape operation per location occurs at a time, preventing redundant scrapes and ensuring cache consistency.
    private readonly SemaphoreSlim _lock = new(1, 1);

    public InMemorySolicitorRepository(ISolicitorScraper scraper, ILogger<InMemorySolicitorRepository> logger)
    {
        _scraper = scraper;
        _logger = logger;
    }

    // Retrieves solicitor data for a given location, using cache if valid unless forceRefresh is true.
    public async Task<LocationResult> GetByLocationAsync(string location, bool forceRefresh, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && TryGetFromCache(location, out var cached))
            {
                _logger.LogDebug("Cache hit for '{Location}'", location);
                return cached! with { FromCache = true };
            }

            return await FetchAndCacheAsync(location, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    // Retrieves solicitor data for multiple locations in parallel, respecting individual cache entries.
    public async Task<IReadOnlyList<LocationResult>> GetByLocationsAsync(ScrapeRequest request, CancellationToken cancellationToken = default)
    {
        // Fan-out scrapes in parallel — each location holds its own lock slot.
        var tasks = request.Locations.Select(loc =>
            GetByLocationAsync(loc, request.ForceRefresh, cancellationToken));

        return await Task.WhenAll(tasks);
    }

    // Invalidates cache for a specific location, forcing next retrieval to scrape anew.
    public void InvalidateCache(string location)
    {
        _cache.Remove(location);
        _logger.LogInformation("Cache invalidated for '{Location}'", location);
    }

    // Clears all cache entries, forcing all locations to scrape anew on next retrieval.
    public void InvalidateAllCaches()
    {
        _cache.Clear();
        _logger.LogInformation("All caches invalidated");
    }

    // Attempts to retrieve a cached result for a location, returning true if valid cache exists.
    private bool TryGetFromCache(string location, out LocationResult? result)
    {
        if (_cache.TryGetValue(location, out var entry) && !entry.IsExpired)
        {
            result = entry.Result;
            return true;
        }
        result = null;
        return false;
    }

    // Performs the scrape for a location, enriches results with "new entrant" flags, and caches the outcome.
    private async Task<LocationResult> FetchAndCacheAsync(string location, CancellationToken cancellationToken)
    {
        IReadOnlyList<Solicitor> scraped;
        string? errorMessage = null;

        try
        {
            scraped = await _scraper.ScrapeAsync(location, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Scrape failed for '{Location}' — returning empty result", location);
            errorMessage = ex.StatusCode.HasValue
                ? $"solicitors.com returned {(int)ex.StatusCode} for '{location}'. The location may not be listed."
                : $"Could not reach solicitors.com for '{location}'. Check your connection.";
            scraped = [];
        }

        // null = no prior scrape exists; only flag new entrants when a baseline is available.
        _seenNames.TryGetValue(location, out var previousNames);

        var enriched = scraped.Select(s => s with
        {
            IsNewlyDiscovered = previousNames != null && !previousNames.Contains(s.Name),
        }).ToList();

        var newNames = new HashSet<string>(scraped.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        _seenNames[location] = newNames;

        var now = DateTimeOffset.UtcNow;
        var result = new LocationResult
        {
            Location = location,
            Solicitors = enriched,
            FromCache = false,
            CachedAt = now,
            NewlyDiscoveredCount = enriched.Count(s => s.IsNewlyDiscovered),
            ErrorMessage = errorMessage,
        };

        _cache[location] = new CacheEntry(result, now.Add(CacheDuration));

        _logger.LogInformation(
            "Cached {Count} solicitors for '{Location}' ({New} new)",
            enriched.Count, location, result.NewlyDiscoveredCount);

        return result;
    }

    // Represents a cached scrape result along with its expiration time. Provides a property to check if the entry is expired.
    private sealed record CacheEntry(LocationResult Result, DateTimeOffset ExpiresAt)
    {
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
