using FluentAssertions;
using InfoTrack.Core.Interfaces;
using InfoTrack.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using InfoTrack.Infrastructure.Repositories;
using Xunit;

namespace InfoTrack.Tests.Repositories;

public sealed class InMemorySolicitorRepositoryTests
{
    private static Solicitor MakeSolicitor(string name, string location = "London") =>
        new() { Name = name, Location = location };

    [Fact]
    public async Task GetByLocationAsync_FirstCall_InvokesScraper()
    {
        var scraper = new Mock<ISolicitorScraper>();
        scraper.Setup(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()))
               .ReturnsAsync([MakeSolicitor("Firm A")]);

        var repo = new InMemorySolicitorRepository(scraper.Object, NullLogger<InMemorySolicitorRepository>.Instance);

        var result = await repo.GetByLocationAsync("London", forceRefresh: false);

        result.Solicitors.Should().HaveCount(1);
        result.FromCache.Should().BeFalse();
        scraper.Verify(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByLocationAsync_SecondCall_ReturnsCached()
    {
        var scraper = new Mock<ISolicitorScraper>();
        scraper.Setup(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()))
               .ReturnsAsync([MakeSolicitor("Firm A")]);

        var repo = new InMemorySolicitorRepository(scraper.Object, NullLogger<InMemorySolicitorRepository>.Instance);

        await repo.GetByLocationAsync("London", forceRefresh: false);
        var second = await repo.GetByLocationAsync("London", forceRefresh: false);

        second.FromCache.Should().BeTrue();
        scraper.Verify(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByLocationAsync_ForceRefresh_BypassesCache()
    {
        var scraper = new Mock<ISolicitorScraper>();
        scraper.Setup(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()))
               .ReturnsAsync([MakeSolicitor("Firm A")]);

        var repo = new InMemorySolicitorRepository(scraper.Object, NullLogger<InMemorySolicitorRepository>.Instance);

        await repo.GetByLocationAsync("London", forceRefresh: false);
        await repo.GetByLocationAsync("London", forceRefresh: true);

        scraper.Verify(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByLocationAsync_FirstCall_NothingIsMarkedNewlyDiscovered()
    {
        var scraper = new Mock<ISolicitorScraper>();
        scraper.Setup(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()))
               .ReturnsAsync([MakeSolicitor("Firm A"), MakeSolicitor("Firm B")]);

        var repo = new InMemorySolicitorRepository(scraper.Object, NullLogger<InMemorySolicitorRepository>.Instance);

        var result = await repo.GetByLocationAsync("London", forceRefresh: false);

        result.Solicitors.Should().AllSatisfy(s => s.IsNewlyDiscovered.Should().BeFalse());
        result.NewlyDiscoveredCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByLocationAsync_NewSolicitorOnRefresh_MarkedAsNewlyDiscovered()
    {
        var scraper = new Mock<ISolicitorScraper>();
        var call = 0;
        scraper.Setup(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()))
               .ReturnsAsync(() =>
               {
                   call++;
                   IReadOnlyList<Solicitor> list = call == 1
                       ? [MakeSolicitor("Firm A")]
                       : [MakeSolicitor("Firm A"), MakeSolicitor("Firm B")];
                   return list;
               });

        var repo = new InMemorySolicitorRepository(scraper.Object, NullLogger<InMemorySolicitorRepository>.Instance);

        await repo.GetByLocationAsync("London", forceRefresh: false);
        var refresh = await repo.GetByLocationAsync("London", forceRefresh: true);

        var firmB = refresh.Solicitors.Single(s => s.Name == "Firm B");
        firmB.IsNewlyDiscovered.Should().BeTrue();

        var firmA = refresh.Solicitors.Single(s => s.Name == "Firm A");
        firmA.IsNewlyDiscovered.Should().BeFalse();

        refresh.NewlyDiscoveredCount.Should().Be(1);
    }

    [Fact]
    public async Task InvalidateCache_ClearsCacheForLocation()
    {
        var scraper = new Mock<ISolicitorScraper>();
        scraper.Setup(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()))
               .ReturnsAsync([MakeSolicitor("Firm A")]);

        var repo = new InMemorySolicitorRepository(scraper.Object, NullLogger<InMemorySolicitorRepository>.Instance);

        await repo.GetByLocationAsync("London", forceRefresh: false);
        repo.InvalidateCache("London");
        await repo.GetByLocationAsync("London", forceRefresh: false);

        scraper.Verify(s => s.ScrapeAsync("London", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByLocationsAsync_MultipleLocs_AllFetched()
    {
        var scraper = new Mock<ISolicitorScraper>();
        scraper.Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((string loc, CancellationToken _) => [MakeSolicitor("Firm", loc)]);

        var repo = new InMemorySolicitorRepository(scraper.Object, NullLogger<InMemorySolicitorRepository>.Instance);
        var request = new ScrapeRequest { Locations = ["London", "Leeds", "Bristol"], ForceRefresh = false };

        var results = await repo.GetByLocationsAsync(request);

        results.Should().HaveCount(3);
        results.Select(r => r.Location).Should().BeEquivalentTo(["London", "Leeds", "Bristol"]);
    }
}