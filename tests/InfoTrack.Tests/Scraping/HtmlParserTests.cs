using FluentAssertions;
using InfoTrack.Infrastructure.Scraping;
using Xunit;

namespace InfoTrack.Tests.Scraping;

public sealed class HtmlParserTests
{
    [Fact]
    public void Parse_SimpleElement_ReturnsCorrectTag()
    {
        var root = HtmlParser.Parse("<div>hello</div>");
        var div = root.FindFirst(n => n.Tag == "div");
        div.Should().NotBeNull();
        div!.InnerText.Should().Be("hello");
    }

    [Fact]
    public void Parse_NestedElements_BuildsCorrectTree()
    {
        const string html = "<div class=\"parent\"><span class=\"child\">text</span></div>";
        var root = HtmlParser.Parse(html);

        var parent = root.FindFirst(n => n.HasClass("parent"));
        parent.Should().NotBeNull();

        var child = parent!.FindFirst(n => n.HasClass("child"));
        child.Should().NotBeNull();
        child!.InnerText.Should().Be("text");
    }

    [Fact]
    public void Parse_HtmlEntities_DecodesCorrectly()
    {
        var root = HtmlParser.Parse("<p>AT&amp;T &amp; Sons</p>");
        var p = root.FindFirst(n => n.Tag == "p");
        p!.InnerText.Should().Be("AT&T & Sons");
    }

    [Fact]
    public void Parse_VoidTags_DoNotConsumeFollowingSiblings()
    {
        const string html = "<div><br/><span>after</span></div>";
        var root = HtmlParser.Parse(html);
        var span = root.FindFirst(n => n.Tag == "span");
        span.Should().NotBeNull();
        span!.InnerText.Should().Be("after");
    }

    [Fact]
    public void HasClass_MultiClassElement_MatchesCorrectly()
    {
        var root = HtmlParser.Parse("<div class=\"foo bar baz\"></div>");
        var div = root.FindFirst(n => n.Tag == "div");
        div!.HasClass("bar").Should().BeTrue();
        div.HasClass("qux").Should().BeFalse();
    }

    [Fact]
    public void FindAll_ReturnsAllMatchingDescendants()
    {
        const string html = "<ul><li class=\"item\">a</li><li class=\"item\">b</li><li>c</li></ul>";
        var root = HtmlParser.Parse(html);
        var items = root.FindAll(n => n.HasClass("item")).ToList();
        items.Should().HaveCount(2);
    }

    [Fact]
    public void GetAttribute_ReturnsAttributeValue()
    {
        var root = HtmlParser.Parse("<a href=\"tel:01234567890\">call</a>");
        var a = root.FindFirst(n => n.Tag == "a");
        a!.GetAttribute("href").Should().Be("tel:01234567890");
    }

    [Fact]
    public void DirectText_ExcludesChildElementText()
    {
        const string html = "<span class=\"h2\">Smith & Co<span class=\"stars\">★★★</span></span>";
        var root = HtmlParser.Parse(html);
        var h2 = root.FindFirst(n => n.HasClass("h2"));
        h2!.DirectText().Should().Be("Smith & Co");
    }

    [Fact]
    public void Parse_SelfClosingTags_HandledCorrectly()
    {
        const string html = "<div><input type=\"text\" /><p>content</p></div>";
        var root = HtmlParser.Parse(html);
        var p = root.FindFirst(n => n.Tag == "p");
        p!.InnerText.Should().Be("content");
    }

    [Fact]
    public void Parse_Comments_AreIgnored()
    {
        const string html = "<div><!-- ignored -->visible</div>";
        var root = HtmlParser.Parse(html);
        var div = root.FindFirst(n => n.Tag == "div");
        div!.InnerText.Trim().Should().Be("visible");
    }

    [Theory]
    [InlineData("<p>a</p><p>b</p>", 2)]
    [InlineData("<p>only</p>", 1)]
    [InlineData("no tags", 0)]
    public void FindAll_ByTag_ReturnsExpectedCount(string html, int expected)
    {
        var root = HtmlParser.Parse(html);
        root.FindAll(n => n.Tag == "p").Should().HaveCount(expected);
    }
}
