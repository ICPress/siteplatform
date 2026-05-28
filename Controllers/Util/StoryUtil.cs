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

        if (storyPublished.StylingInfo?.Spans == null ||
            !storyPublished.StylingInfo.Spans.Any())
        {
            return HttpUtility.HtmlEncode(storyPublished.ContentText)
                .Replace("\n", "<br>");
        }

        var text = storyPublished.ContentText;

        var events = new List<SpanEvent>();

        foreach (SpanInfoModel span in storyPublished.StylingInfo.Spans)
        {
            // IMAGE only inserts once
            if (span.Style == TextStyleModel.IMAGE)
            {
                events.Add(new SpanEvent
                {
                    Position = span.Start,
                    IsStart = true,
                    Span = span
                });

                continue;
            }

            events.Add(new SpanEvent
            {
                Position = span.Start,
                IsStart = true,
                Span = span
            });

            events.Add(new SpanEvent
            {
                Position = span.End,
                IsStart = false,
                Span = span
            });
        }

        var orderedEvents = events
            .OrderBy(e => e.Position)
            // Closing tags first at same position
            .ThenBy(e => e.IsStart ? 1 : 0)
            .ThenBy(e => GetStylePriority(
                e.Span.Style,
                e.IsStart))
            .ToList();

        var sb = new StringBuilder();

        int currentIndex = 0;

        foreach (var ev in orderedEvents)
        {
            if (ev.Position > currentIndex)
            {
                var chunk = text.Substring(
                    currentIndex,
                    ev.Position - currentIndex);

                sb.Append(HttpUtility.HtmlEncode(chunk));

                currentIndex = ev.Position;
            }

            sb.Append(GetHtmlTag(
                ev,
                bigCDN,
                smallCDN));
        }

        // Remaining text
        if (currentIndex < text.Length)
        {
            sb.Append(HttpUtility.HtmlEncode(
                text.Substring(currentIndex)));
        }

        return sb.Replace("</h3>\n", "</h3>").Replace("\n", "<br>").ToString();   
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
}