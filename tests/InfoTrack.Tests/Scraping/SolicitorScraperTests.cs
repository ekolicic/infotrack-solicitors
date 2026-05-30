using FluentAssertions;
using InfoTrack.Infrastructure.Scraping;

namespace InfoTrack.Tests.Scraping;

public sealed class SolicitorScraperTests
{
    private const string SampleResultItem = """
        <div class="result-item">
          <span class="h2">Blackstone Legal Ltd<span class="stars">★★★★</span></span>
          <a class="link-map" href="/map">
            <address>12 High Street, London, EC1A 1BB</address>
          </a>
          <div class="phone-block">
            <a href="tel:02071234567">020 7123 4567</a>
          </div>
          <ul class="list-item">
            <li><a target="_blank" href="https://blackstonelegal.co.uk" rel="nofollow"><i class="fa fa-globe" aria-hidden="true"></i>Website</a></li>
          </ul>
          <span class="rev-results">
            <div class="star-full"></div>
            <div class="star-full"></div>
            <div class="star-full"></div>
            <div class="star-half"></div>
          </span>
          <span class="rev-count">(42 reviews)</span>
        </div>
        """;

    [Fact]
    public void Parser_ExtractsFirmName_ExcludingStarSpan()
    {
        var root = HtmlParser.Parse(SampleResultItem);
        var h2 = root.FindFirst(n => n.Tag == "span" && n.HasClass("h2"));
        h2!.DirectText().Should().Be("Blackstone Legal Ltd");
    }

    [Fact]
    public void Parser_ExtractsAddress()
    {
        var root = HtmlParser.Parse(SampleResultItem);
        var mapLink = root.FindFirst(n => n.Tag == "a" && n.HasClass("link-map"));
        var address = mapLink!.FindFirst(n => n.Tag == "address");
        address!.InnerText.Trim().Should().Contain("12 High Street");
    }

    [Fact]
    public void Parser_ExtractsTelephoneFromHref()
    {
        var root = HtmlParser.Parse(SampleResultItem);
        var telNode = root.FindFirst(n =>
            n.Tag == "a" &&
            n.GetAttribute("href")?.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) == true);

        telNode.Should().NotBeNull();
        telNode!.GetAttribute("href").Should().Be("tel:02071234567");
    }

    [Fact]
    public void Parser_ExtractsWebsite()
    {
        var root = HtmlParser.Parse(SampleResultItem);
        var link = root.FindFirst(n =>
            n.Tag == "a" &&
            n.FindFirst(c => c.Tag == "i" && c.HasClass("fa-globe")) is not null);
        link!.GetAttribute("href").Should().Be("https://blackstonelegal.co.uk");
    }

    [Fact]
    public void Parser_ExtractsStarRating_ThreeAndHalf()
    {
        var root = HtmlParser.Parse(SampleResultItem);
        var revResults = root.FindFirst(n => n.Tag == "span" && n.HasClass("rev-results"));
        int full = revResults!.FindAll(n => n.Tag == "div" && n.HasClass("star-full")).Count();
        int half = revResults.FindAll(n => n.Tag == "div" && n.HasClass("star-half")).Count();
        (full + half * 0.5m).Should().Be(3.5m);
    }

    [Fact]
    public void Parser_ExtractsReviewCount()
    {
        var root = HtmlParser.Parse(SampleResultItem);
        var revCount = root.FindFirst(n => n.Tag == "span" && n.HasClass("rev-count"));
        var text = System.Text.RegularExpressions.Regex.Replace(revCount!.InnerText, @"[^\d]", "");
        int.Parse(text).Should().Be(42);
    }

    [Fact]
    public void Parser_MultipleResultItems_ParsedIndependently()
    {
        const string html = """
            <div class="results">
              <div class="result-item"><span class="h2">Firm A</span></div>
              <div class="result-item"><span class="h2">Firm B</span></div>
            </div>
            """;

        var root = HtmlParser.Parse(html);
        var items = root.FindAll(n => n.Tag == "div" && n.HasClass("result-item")).ToList();
        items.Should().HaveCount(2);
        items[0].FindFirst(n => n.HasClass("h2"))!.InnerText.Should().Be("Firm A");
        items[1].FindFirst(n => n.HasClass("h2"))!.InnerText.Should().Be("Firm B");
    }

    [Fact]
    public void Parser_EmptyResultSet_ReturnsNoItems()
    {
        const string html = "<div class=\"results\"><p>No results found</p></div>";
        var root = HtmlParser.Parse(html);
        root.FindAll(n => n.HasClass("result-item")).Should().BeEmpty();
    }

    // Fixture taken directly from a live solicitors.com result-item to verify
    // the parser handles real-world markup — notably the greentick <div> inside
    // span.h2, and a firm with no star-rating data.
    private const string RealWorldResultItem = """
        <div class="result-item">
            <div class="top-holder">
                <span class="h2">Amphlett Lissimore<div class="greentick" title="quality marks"></div></span>
                <div class="phone-block mobile-hidden">
                    <span>Phone:</span>
                    <a rel="noindex" href="tel:02087715254">020 8771 5254</a>
                </div>
            </div>
            <a href="/amphlett-lissimore.html" class="link-map"><i class="fa fa-map-marker" aria-hidden="true"></i>
                <address>Greystoke House, 80-86 Westow Street, London, SE19 3AF</address>
            </a>
            <ul class="list-item">
                <li><a target="_blank" href="http://www.allaw.co.uk" rel="nofollow"><i class="fa fa-globe" aria-hidden="true"></i>Website</a></li>
            </ul>
        </div>
        """;

    [Fact]
    public void RealWorldFixture_FirmName_ExcludesGreentickDiv()
    {
        var root = HtmlParser.Parse(RealWorldResultItem);
        var h2 = root.FindFirst(n => n.Tag == "span" && n.HasClass("h2"));
        h2!.DirectText().Trim().Should().Be("Amphlett Lissimore");
    }

    [Fact]
    public void RealWorldFixture_Address_ParsedCorrectly()
    {
        var root = HtmlParser.Parse(RealWorldResultItem);
        var address = root.FindFirst(n => n.Tag == "address");
        address!.InnerText.Trim().Should().Be("Greystoke House, 80-86 Westow Street, London, SE19 3AF");
    }

    [Fact]
    public void RealWorldFixture_NoRating_ReturnsNull()
    {
        var root = HtmlParser.Parse(RealWorldResultItem);
        var revResults = root.FindFirst(n => n.Tag == "span" && n.HasClass("rev-results"));
        revResults.Should().BeNull();
    }

    [Fact]
    public void RealWorldFixture_Website_ParsedCorrectly()
    {
        var root = HtmlParser.Parse(RealWorldResultItem);
        var link = root.FindFirst(n =>
            n.Tag == "a" &&
            n.FindFirst(c => c.Tag == "i" && c.HasClass("fa-globe")) is not null);
        link!.GetAttribute("href").Should().Be("http://www.allaw.co.uk");
    }
}
