using InfoTrack.Core.Interfaces;
using InfoTrack.Core.Models;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Scraping;

public sealed class SolicitorScraper : ISolicitorScraper
{
    private const string BaseUrl = "https://www.solicitors.com/conveyancing+{0}.html";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SolicitorScraper> _logger;

    public SolicitorScraper(IHttpClientFactory httpClientFactory, ILogger<SolicitorScraper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Solicitor>> ScrapeAsync(string location, CancellationToken cancellationToken = default)
    {
        var slug = BuildSlug(location);
        var url = string.Format(BaseUrl, slug);
        _logger.LogInformation("Scraping {Url}", url);

        string html;
        try
        {
            var client = _httpClientFactory.CreateClient("Scraper");
            var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("No page found for location '{Location}' at {Url}", location, url);
                return [];
            }

            response.EnsureSuccessStatusCode();
            html = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for location '{Location}'", location);
            throw;
        }

        return ParseResults(html, location);
    }

    internal static string BuildSlug(string location) =>
        location.Trim().ToLowerInvariant();

    private static IReadOnlyList<Solicitor> ParseResults(string html, string location)
    {
        var root = HtmlParser.Parse(html);
        var solicitors = new List<Solicitor>();

        var resultItems = root.AllWithClass("div", "result-item");

        foreach (var item in resultItems)
        {
            var name = ExtractFirmName(item);
            if (string.IsNullOrWhiteSpace(name)) continue;

            solicitors.Add(new Solicitor
            {
                Name = name,
                Address = ExtractAddress(item),
                Telephone = ExtractTelephone(item),
                Website = ExtractWebsite(item),
                Location = location,
                StarRating = ExtractStarRating(item),
                ReviewCount = ExtractReviewCount(item),
                ScrapedAt = DateTimeOffset.UtcNow,
            });
        }

        return solicitors;
    }

    private static string? ExtractFirmName(HtmlNode item)
    {
        //e.g. <span class="h2">Smith & Co Solicitors</span>
        var h2 = item.FindFirst(n => n.Tag == "span" && n.HasClass("h2"));
        return h2?.DirectText();
    }

    private static string? ExtractAddress(HtmlNode item)
    {
        //e.g. <a class="link-map" href="https://maps.google.com/?q=123+Main+St"><address>123 Main St, Anytown</address></a>
        var mapLink = item.FindFirst(n => n.Tag == "a" && n.HasClass("link-map"));
        var address = mapLink?.FindFirst(n => n.Tag == "address");
        return address?.InnerText.Trim().Replace("\n", ", ").Replace("\r", "");
    }

    private static string? ExtractTelephone(HtmlNode item)
    {
        //e.g. <a rel = "noindex" href="tel: 02083707750">0208 370 7750</a>
        var telNode = item.FindFirst(n =>
            n.Tag == "a" &&
            n.GetAttribute("href")?.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) == true);

        if (telNode is null) return null;

        var href = telNode.GetAttribute("href")!;
        var number = href["tel:".Length..].Trim();
        return string.IsNullOrWhiteSpace(number) ? telNode.InnerText.Trim() : number;
    }

    private static string? ExtractWebsite(HtmlNode item)
    {
        // e.g. <a href="https://www.example.com" rel="nofollow"><i class="fa-solid fa-globe"></i></a>
        var siteLink = item.FindFirst(n =>
            n.Tag == "a" &&
            n.FindFirst(c => c.Tag == "i" && c.HasClass("fa-globe")) is not null);
        return siteLink?.GetAttribute("href");
    }

    private static decimal? ExtractStarRating(HtmlNode item)
    {
        //e.g. <span class="rev-results"><div class="star-full"></div><div class="star-full"></div><div class="star-full"></div><div class="star-half"></div><div class="star-empty"></div></span>
        var revResults = item.FindFirst(n => n.Tag == "span" && n.HasClass("rev-results"));
        if (revResults is null) return null;

        int full = revResults.FindAll(n => n.Tag == "div" && n.HasClass("star-full")).Count();
        int half = revResults.FindAll(n => n.Tag == "div" && n.HasClass("star-half")).Count();

        return (full == 0 && half == 0) ? null : full + half * 0.5m;
    }

    private static int? ExtractReviewCount(HtmlNode item)
    {
        // e.g. <span class="rev-count">12 reviews</span>
        var revCount = item.FindFirst(n => n.Tag == "span" && n.HasClass("rev-count"));
        if (revCount is null) return null;

        var text = System.Text.RegularExpressions.Regex.Replace(revCount.InnerText, @"[^\d]", "");
        return int.TryParse(text, out var count) ? count : null;
    }
}
