using InfoTrack.Core.Interfaces;
using InfoTrack.Infrastructure.Repositories;
using InfoTrack.Infrastructure.Scraping;
using Microsoft.Extensions.DependencyInjection;

namespace InfoTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient("Scraper", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/124.0.0.0 Safari/537.36");

            client.DefaultRequestHeaders.Accept.ParseAdd(
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");

            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<ISolicitorScraper, SolicitorScraper>();
        services.AddSingleton<ISolicitorRepository, InMemorySolicitorRepository>();

        return services;
    }
}