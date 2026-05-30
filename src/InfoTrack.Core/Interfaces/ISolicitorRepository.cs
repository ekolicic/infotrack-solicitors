using InfoTrack.Core.Models;

namespace InfoTrack.Core.Interfaces;

public interface ISolicitorRepository
{
    Task<LocationResult> GetByLocationAsync(string location, bool forceRefresh, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationResult>> GetByLocationsAsync(ScrapeRequest request, CancellationToken cancellationToken = default);
    void InvalidateCache(string location);
    void InvalidateAllCaches();
}

