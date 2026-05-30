using InfoTrack.Core.Models;
using System;

namespace InfoTrack.Infrastructure.Scraping;

//TODO: remove it
//<div class="result-item">
//	<div class="top-holder">
//		<span class="h2">Aspen Morris Solicitors</span>
//		<div class="phone-block mobile-hidden">
//			<span>Phone:</span>
//			<a rel = "noindex" href="tel:02083707750">0208 370 7750</a>
//		</div>

//		<div class="logo-holder mobile-hidden" id="SiD63429PiD0">
//			<a href = "#" >< img src="/logos/aspen-morris-solicitors.jpg" alt="Aspen Morris Solicitors"></a>
//		</div>

//	</div>
//	<a href = "/aspen-morris-solicitors.html" class="link-map"><i class="fa fa-map-marker" aria-hidden="true"></i>
//		<address>141 High Street, Southgate, London N14 6BP</address>
//	</a>
//	<p>Aspen Morris Solicitors provide Conveyancing legal solutions in London to meet your business and individual
//        needs, call us today to find out how we can help.</p>
//	<ul class="list-item">
//		<li class="red-color"><a rel = "noindex nofollow" href="/enquiry-form.asp?SiD=NjM0Mjk&amp;DiD=MTky"><i
//                    class="fa fa-envelope" aria-hidden="true"></i>Email</a></li>
//		<li><a target = "_blank" href="http://www.aspenmorris.com/" rel="nofollow"><i class="fa fa-globe"
//					aria-hidden="true"></i>Website</a></li>

//		<li class="blue-color mobile-hidden"><i class="fa fa-chevron-right" aria-hidden="true"></i>View more info</li>
//	</ul>
//</div>

public static class HtmlParser
{
    public static HtmlNode Parse(string html) => new TreeBuilder(html).Build();
}

public sealed class HtmlNode
{
    public string Tag { get; init; } = string.Empty;
    public string InnerText { get; internal set; } = string.Empty;
    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<HtmlNode> Children { get; private set; } = [];

    internal void SetChildren(List<HtmlNode> children)
    {
        Children = children;
        InnerText = BuildInnerText(children);
    }

    private static string BuildInnerText(IReadOnlyList<HtmlNode> nodes)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var n in nodes)
        {
            if (n.Tag == "#text")
                sb.Append(n.InnerText);
            else
                sb.Append(BuildInnerText(n.Children));
        }
        return HtmlEntities.Decode(sb.ToString());
    }

    public string? GetAttribute(string name) =>
        Attributes.TryGetValue(name.ToLowerInvariant(), out var v) ? v : null;

    public bool HasClass(string className)
    {
        var cls = GetAttribute("class");
        if (cls is null) return false;
        return Array.Exists(cls.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            c => c.Equals(className, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<HtmlNode> DescendantsAndSelf()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var d in child.DescendantsAndSelf())
                yield return d;
    }

    public HtmlNode? FindFirst(Func<HtmlNode, bool> predicate) =>
        DescendantsAndSelf().FirstOrDefault(predicate);

    public IEnumerable<HtmlNode> FindAll(Func<HtmlNode, bool> predicate) =>
        DescendantsAndSelf().Where(predicate);

    public HtmlNode? FirstWithClass(string tag, string cssClass) =>
        FindFirst(n => n.Tag == tag && n.HasClass(cssClass));

    public IEnumerable<HtmlNode> AllWithClass(string tag, string cssClass) =>
        FindAll(n => n.Tag == tag && n.HasClass(cssClass));

    /// <summary>Returns the direct text content of this node only (no child element text).</summary>
    public string DirectText()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in Children)
            if (c.Tag == "#text") sb.Append(c.InnerText);
        return HtmlEntities.Decode(sb.ToString()).Trim();
    }
}

/// <summary>Converts common HTML entities to their character equivalents.</summary>
internal static class HtmlEntities
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["amp"] = "&",
        ["lt"] = "<",
        ["gt"] = ">",
        ["quot"] = "\"",
        ["apos"] = "'",
        ["nbsp"] = " ",
        ["copy"] = "©",
        ["reg"] = "®",
        ["trade"] = "™",
        ["mdash"] = "—",
        ["ndash"] = "–",
        ["lsquo"] = "‘",
        ["rsquo"] = "’",
        ["ldquo"] = "“",
        ["rdquo"] = "”",
    };

    public static string Decode(string input)
    {
        if (!input.Contains('&')) return input;

        var sb = new System.Text.StringBuilder(input.Length);
        int i = 0;
        while (i < input.Length)
        {
            if (input[i] != '&')
            {
                sb.Append(input[i++]);
                continue;
            }
            int semi = input.IndexOf(';', i + 1);
            if (semi < 0 || semi - i > 10) { sb.Append(input[i++]); continue; }

            var entity = input.Substring(i + 1, semi - i - 1);
            if (entity.StartsWith('#'))
            {
                if (int.TryParse(entity[1..], out int code))
                    sb.Append((char)code);
                else
                    sb.Append(input, i, semi - i + 1);
            }
            else if (_map.TryGetValue(entity, out var replacement))
                sb.Append(replacement);
            else
                sb.Append(input, i, semi - i + 1);

            i = semi + 1;
        }
        return sb.ToString();
    }
}

/// <summary>Builds the node tree by tokenising an HTML string.</summary>
internal sealed class TreeBuilder
{
    private readonly string _html;
    private int _pos;

    // Tags that are void (self-closing) and never have children.
    private static readonly HashSet<string> _voidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area","base","br","col","embed","hr","img","input","link","meta","param","source","track","wbr"
    };

    public TreeBuilder(string html) => _html = html;

    public HtmlNode Build()
    {
        var root = new HtmlNode { Tag = "#document" };
        var children = ParseChildren(isRoot: true);
        root.SetChildren(children);
        return root;
    }

    private List<HtmlNode> ParseChildren(bool isRoot = false, string? parentTag = null)
    {
        var nodes = new List<HtmlNode>();

        while (_pos < _html.Length)
        {
            if (_html[_pos] == '<')
            {
                // Closing tag — stop collecting children for the parent.
                if (_pos + 1 < _html.Length && _html[_pos + 1] == '/')
                {
                    if (isRoot) { _pos++; continue; } // malformed — skip
                    SkipClosingTag();
                    return nodes;
                }

                // Comment or CDATA
                if (_html.AsSpan(_pos).StartsWith("<!--"))
                {
                    SkipUntil("-->");
                    continue;
                }
                if (_html.AsSpan(_pos).StartsWith("<!["))
                {
                    SkipUntil("]]>");
                    continue;
                }

                var element = ParseOpenTag();
                if (element is null) continue;

                if (!_voidTags.Contains(element.Tag) &&
                    element.GetAttribute("self-closing-marker") != "true")
                {
                    var children = ParseChildren(parentTag: element.Tag);
                    element.SetChildren(children);
                }

                nodes.Add(element);
            }
            else
            {
                var text = ParseText();
                if (!string.IsNullOrWhiteSpace(text))
                    nodes.Add(new HtmlNode { Tag = "#text", InnerText = text });
            }
        }

        return nodes;
    }

    private HtmlNode? ParseOpenTag()
    {
        _pos++; // skip '<'
        if (_pos >= _html.Length) return null;

        var tag = ReadTagName();
        if (string.IsNullOrEmpty(tag)) { SkipUntil(">"); return null; }

        var attrs = ReadAttributes(out bool selfClosing);

        // special: skip script/style content wholesale
        if (tag.Equals("script", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            SkipUntil($"</{tag}");
            SkipUntil(">");
            return null;
        }

        var node = new HtmlNode { Tag = tag.ToLowerInvariant(), Attributes = attrs };
        if (selfClosing) node.SetChildren([]);
        return node;
    }

    private string ReadTagName()
    {
        int start = _pos;
        while (_pos < _html.Length && !char.IsWhiteSpace(_html[_pos]) && _html[_pos] != '>' && _html[_pos] != '/')
            _pos++;
        return _html.Substring(start, _pos - start);
    }

    private Dictionary<string, string> ReadAttributes(out bool selfClosing)
    {
        selfClosing = false;
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        while (_pos < _html.Length)
        {
            SkipWhitespace();
            if (_pos >= _html.Length) break;

            if (_html[_pos] == '>')
            {
                _pos++;
                break;
            }

            if (_html[_pos] == '/' && _pos + 1 < _html.Length && _html[_pos + 1] == '>')
            {
                selfClosing = true;
                _pos += 2;
                break;
            }

            var name = ReadAttrName();
            if (string.IsNullOrEmpty(name)) { _pos++; continue; }

            SkipWhitespace();
            if (_pos < _html.Length && _html[_pos] == '=')
            {
                _pos++;
                SkipWhitespace();
                var value = ReadAttrValue();
                attrs[name.ToLowerInvariant()] = value;
            }
            else
            {
                attrs[name.ToLowerInvariant()] = name;
            }
        }

        return attrs;
    }

    private string ReadAttrName()
    {
        int start = _pos;
        while (_pos < _html.Length && !char.IsWhiteSpace(_html[_pos]) &&
               _html[_pos] != '=' && _html[_pos] != '>' && _html[_pos] != '/')
            _pos++;
        return _html.Substring(start, _pos - start);
    }

    private string ReadAttrValue()
    {
        if (_pos >= _html.Length) return string.Empty;

        char quote = _html[_pos];
        if (quote == '"' || quote == '\'')
        {
            _pos++;
            int start = _pos;
            while (_pos < _html.Length && _html[_pos] != quote)
                _pos++;
            var value = _html.Substring(start, _pos - start);
            if (_pos < _html.Length) _pos++;
            return value;
        }

        // Unquoted attribute value
        int us = _pos;
        while (_pos < _html.Length && !char.IsWhiteSpace(_html[_pos]) && _html[_pos] != '>')
            _pos++;
        return _html.Substring(us, _pos - us);
    }

    private string ParseText()
    {
        int start = _pos;
        while (_pos < _html.Length && _html[_pos] != '<')
            _pos++;
        return _html.Substring(start, _pos - start);
    }

    private void SkipWhitespace()
    {
        while (_pos < _html.Length && char.IsWhiteSpace(_html[_pos]))
            _pos++;
    }

    private void SkipUntil(string marker)
    {
        int idx = _html.IndexOf(marker, _pos, StringComparison.OrdinalIgnoreCase);
        _pos = idx < 0 ? _html.Length : idx + marker.Length;
    }

    private void SkipClosingTag()
    {
        SkipUntil(">");
    }
}
