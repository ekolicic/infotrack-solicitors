using InfoTrack.Core.Models;

namespace InfoTrack.Core.Interfaces;

public interface ISolicitorScraper
{
    Task<IReadOnlyList<Solicitor>> ScrapeAsync(string location, CancellationToken cancellationToken = default);
}
