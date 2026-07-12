using System.Text;
using System.Text.Json;
using System.Web;

namespace siteplatform.Util;

public static class StoryUtil
{
    public static string GetFormatedArticleText(
        StoryPublishedModel storyPublished,
        string bigCDN,
        string smallCDN)
    {
        if (string.IsNullOrWhiteSpace(storyPublished.ContentText))
            return string.Empty;

        var text = storyPublished.ContentText;

        var hasSpans = storyPublished.StylingInfo?.Spans != null &&
                       storyPublished.StylingInfo.Spans.Any();

        var hasCitations = storyPublished.Sources != null &&
                            storyPublished.Sources.Any(s => s.References != null && s.References.Any());

        if (!hasSpans && !hasCitations)
        {
            return HttpUtility.HtmlEncode(text).Replace("\n", "<br>");
        }

        var items = new List<RenderItem>();

        if (hasSpans)
        {
            foreach (SpanInfoModel span in storyPublished.StylingInfo!.Spans)
            {
                // IMAGE only inserts once
                if (span.Style == TextStyleModel.IMAGE)
                {
                    var imageEv = new SpanEvent { Position = span.Start, IsStart = true, Span = span };
                    items.Add(new RenderItem
                    {
                        Position = span.Start,
                        // Same rank family as other opening tags (default priority)
                        Rank = 20 + GetStylePriority(span.Style, true),
                        Render = () => GetHtmlTag(imageEv, bigCDN, smallCDN)
                    });

                    continue;
                }

                var startEv = new SpanEvent { Position = span.Start, IsStart = true, Span = span };
                items.Add(new RenderItem
                {
                    Position = span.Start,
                    Rank = 20 + GetStylePriority(span.Style, true),
                    Render = () => GetHtmlTag(startEv, bigCDN, smallCDN)
                });

                var endEv = new SpanEvent { Position = span.End, IsStart = false, Span = span };
                items.Add(new RenderItem
                {
                    Position = span.End,
                    Rank = GetStylePriority(span.Style, false),
                    Render = () => GetHtmlTag(endEv, bigCDN, smallCDN)
                });
            }
        }

        if (hasCitations)
        {
            var citationsByPosition = BuildCitationsByPosition(storyPublished.Sources!, text.Length);

            foreach (var kvp in citationsByPosition)
            {
                var position = kvp.Key;
                var entries = kvp.Value;

                items.Add(new RenderItem
                {
                    Position = position,
                    // Sits between closing tags (0-4) and opening tags (20+) at the same position
                    Rank = 10,
                    Render = () => RenderCitationMarker(entries)
                });
            }
        }

        var orderedItems = items
            .OrderBy(i => i.Position)
            .ThenBy(i => i.Rank)
            .ToList();

        var sb = new StringBuilder();

        int currentIndex = 0;

        foreach (var item in orderedItems)
        {
            if (item.Position > currentIndex)
            {
                var chunk = text.Substring(
                    currentIndex,
                    item.Position - currentIndex);

                sb.Append(HttpUtility.HtmlEncode(chunk));

                currentIndex = item.Position;
            }

            sb.Append(item.Render());
        }

        // Remaining text
        if (currentIndex < text.Length)
        {
            sb.Append(HttpUtility.HtmlEncode(
                text.Substring(currentIndex)));
        }

        return sb.Replace("</h3>\n", "</h3>").Replace("\n", "<br>").ToString();
    }

    /// <summary>
    /// Builds the "Sources" list shown at the bottom of the article, e.g.:
    /// &lt;ol class="references"&gt;&lt;li id="ref-1"&gt;Reuters. "&lt;a href="..."&gt;Headline&lt;/a&gt;."&lt;/li&gt;...&lt;/ol&gt;
    /// The footnote index (ref-N) matches the numbers used by inline citation markers,
    /// based on each source's position in the Sources list. If SourceName is empty, the
    /// source's URL host (minus "www.") is used instead.
    /// </summary>
    public static string GetReferencesListHtml(List<SourceModel>? sources)
    {
        if (sources == null || !sources.Any())
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append("<ol class=\"references\">");

        for (int i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var footnoteIndex = i + 1;

            var name = HttpUtility.HtmlEncode(GetDisplaySourceName(source));
            var urlTitle = HttpUtility.HtmlEncode(source.UrlTitle ?? "");
            var url = HttpUtility.HtmlAttributeEncode(source.Url ?? "");

            sb.Append($"<li id=\"ref-{footnoteIndex}\">");
            sb.Append(name);
            sb.Append(". \"");
            sb.Append($"<a href=\"{url}\" target=\"_blank\" rel=\"nofollow noopener\">{urlTitle}</a>");
            sb.Append(".\"");
            sb.Append("</li>");
        }

        sb.Append("</ol>");

        return sb.ToString();
    }

    /// <summary>
    /// Groups every source's reference facts by the exact ContentText position(s) they cite.
    /// Each ReferenceModel.Index entry is an explicit character offset into ContentText where
    /// a marker for that source/sentence pair belongs (a single fact can be cited at more than
    /// one spot in the text). Multiple facts from the same source landing on the same position
    /// collapse into a single citation entry (their sentences are aggregated for the tooltip).
    /// </summary>
    private static Dictionary<int, List<CitationEntry>> BuildCitationsByPosition(
        List<SourceModel> sources,
        int contentLength)
    {
        var result = new Dictionary<int, List<CitationEntry>>();

        for (int i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var footnoteIndex = i + 1;

            if (source.References == null || !source.References.Any())
                continue;

            foreach (var reference in source.References)
            {
                if (string.IsNullOrWhiteSpace(reference.Sentence) ||
                    reference.Index == null || !reference.Index.Any())
                {
                    continue;
                }

                foreach (var rawPosition in reference.Index.Distinct())
                {
                    var position = Math.Clamp(rawPosition, 0, contentLength);

                    if (!result.TryGetValue(position, out var list))
                    {
                        list = new List<CitationEntry>();
                        result[position] = list;
                    }

                    var existingEntry = list.FirstOrDefault(e => e.FootnoteIndex == footnoteIndex);
                    if (existingEntry != null)
                    {
                        if (!existingEntry.Sentences.Contains(reference.Sentence))
                            existingEntry.Sentences.Add(reference.Sentence);
                    }
                    else
                    {
                        list.Add(new CitationEntry
                        {
                            FootnoteIndex = footnoteIndex,
                            SourceName = GetDisplaySourceName(source),
                            Url = source.Url ?? "",
                            Sentences = new List<string> { reference.Sentence }
                        });
                    }
                }
            }
        }

        foreach (var key in result.Keys.ToList())
        {
            result[key] = result[key].OrderBy(e => e.FootnoteIndex).ToList();
        }

        return result;
    }

    /// <summary>
    /// Renders an inline citation marker, e.g. "[1, 2]" — each number links to its footnote
    /// and, on click, opens a floating tooltip with the sentence(s)/source for that citation.
    /// </summary>
    private static string RenderCitationMarker(List<CitationEntry> entries)
    {
        if (entries == null || !entries.Any())
            return string.Empty;

        var links = entries.Select(e =>
        {
            var sentencesJson = JsonSerializer.Serialize(e.Sentences);
            var dataSentences = HttpUtility.HtmlAttributeEncode(sentencesJson);
            var dataSource = HttpUtility.HtmlAttributeEncode(e.SourceName);
            var dataUrl = HttpUtility.HtmlAttributeEncode(e.Url);

            return
                $"<a href=\"#ref-{e.FootnoteIndex}\" class=\"ref-note\" " +
                $"data-source=\"{dataSource}\" data-url=\"{dataUrl}\" " +
                $"data-sentences=\"{dataSentences}\" " +
                $"onclick=\"showRefPopup(event, this)\">{e.FootnoteIndex}</a>";
        });

        return "<sup class=\"ref-group\">[" + string.Join(", ", links) + "]</sup>";
    }

    /// <summary>
    /// Returns SourceName if present; otherwise falls back to the source URL's host
    /// (domain + top-level domain, with a leading "www." stripped), e.g. "reuters.com".
    /// </summary>
    private static string GetDisplaySourceName(SourceModel source)
    {
        if (!string.IsNullOrWhiteSpace(source.SourceName))
            return source.SourceName;

        if (string.IsNullOrWhiteSpace(source.Url))
            return "";

        if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri))
            return "";

        var host = uri.Host;

        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            host = host.Substring(4);

        return host;
    }

    private static string GetHtmlTag(
        SpanEvent ev,
        string bigCDN,
        string smallCDN)
    {
        var span = ev.Span;

        switch (span.Style)
        {
            case TextStyleModel.BOLD:
                return ev.IsStart
                    ? "<b>"
                    : "</b>";

            case TextStyleModel.ITALIC:
                return ev.IsStart
                    ? "<i>"
                    : "</i>";

            case TextStyleModel.UNDERLINE:
                return ev.IsStart
                    ? "<u>"
                    : "</u>";

            case TextStyleModel.TEXT_SIZE_LARGE:
                return ev.IsStart
                    ? "<h3>"
                    : "</h3>";

            case TextStyleModel.REFER_LINK:
                if (ev.IsStart)
                {
                    var url = HttpUtility.HtmlAttributeEncode(
                        span.AdditionalInfoFlag ?? "");

                    return $"<a href=\"{url}\" target=\"_blank\">";
                }

                return "</a>";

            case TextStyleModel.IMAGE:
                if (!ev.IsStart)
                    return string.Empty;

                if (string.IsNullOrWhiteSpace(
                    span.AdditionalInfoFlag))
                {
                    return string.Empty;
                }

                var metadata =
                    JsonSerializer.Deserialize<ImageInfoMetadata>(
                        span.AdditionalInfoFlag);

                if (metadata == null)
                    return string.Empty;

                var width =
                    metadata.MinWidth ?? metadata.Width;

                var height =
                    metadata.MinHeight ?? metadata.Height;

                var imageUrl =
                    LinkUtil.GetDefaultImageLinkFromImageInfoMetadataParsed(
                        metadata,
                        bigCDN,
                        smallCDN);

                return
                    $"<br/><img src=\"{HttpUtility.HtmlAttributeEncode(imageUrl)}\" " +
                    $"alt=\"attached image\" " +
                    $"style=\"max-width:{width}px; max-height:{height}px; width:85vw\" /><br/>";

            default:
                return string.Empty;
        }
    }

    private static int GetStylePriority(
        TextStyleModel style,
        bool isStart)
    {
        // Opening tags:
        // outer tags first
        //
        // Closing tags:
        // inner tags first

        if (isStart)
        {
            return style switch
            {
                TextStyleModel.TEXT_SIZE_LARGE => 0,
                TextStyleModel.BOLD => 1,
                TextStyleModel.ITALIC => 2,
                TextStyleModel.UNDERLINE => 3,
                TextStyleModel.REFER_LINK => 4,
                _ => 100
            };
        }

        return style switch
        {
            TextStyleModel.REFER_LINK => 0,
            TextStyleModel.UNDERLINE => 1,
            TextStyleModel.ITALIC => 2,
            TextStyleModel.BOLD => 3,
            TextStyleModel.TEXT_SIZE_LARGE => 4,
            _ => 100
        };
    }

    private sealed class SpanEvent
    {
        public int Position { get; set; }

        public bool IsStart { get; set; }

        public SpanInfoModel Span { get; set; } = default!;
    }

    /// <summary>
    /// Unified insertion point used to interleave style tags and citation markers in
    /// position order. Rank breaks ties at the same Position: closing style tags (0-4)
    /// render first, then citation markers (10), then opening style tags (20+).
    /// </summary>
    private sealed class RenderItem
    {
        public int Position { get; set; }

        public int Rank { get; set; }

        public Func<string> Render { get; set; } = () => string.Empty;
    }

    private sealed class CitationEntry
    {
        public int FootnoteIndex { get; set; }

        public string SourceName { get; set; } = "";

        public string Url { get; set; } = "";

        public List<string> Sentences { get; set; } = new List<string>();
    }
}
