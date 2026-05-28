using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SimpleMvcSitemap;
using SimpleMvcSitemap.Images;
using SimpleMvcSitemap.News;
using siteplatform.Util;

namespace siteplatform.Controllers;

public class SitemapController : Controller
{
    private readonly ISitemapProvider _sitemapProvider;
    private readonly ILogger<SitemapController> _logger;
    private readonly ServerSettings _serverSettings;

    private const string PublicationName = "ICPress";

    public SitemapController(
        ISitemapProvider sitemapProvider,
        ILogger<SitemapController> logger,
        ServerSettings serverSettings)
    {
        _sitemapProvider = sitemapProvider;
        _logger = logger;
        _serverSettings = serverSettings;
    }

    public async Task<ActionResult> Index()
    {
        var nodes = new List<SitemapNode>(300);
        var api      = _serverSettings.PublicApiUrl;
        var smallCDN = _serverSettings.CDNRequestSmallPath;
        var bigCDN   = _serverSettings.CDNRequestLargePath;

        try
        {
            using var httpClient = new HttpClient();

            // Fetch latest 18 — included in Google News sitemap with full metadata
            var latest = await httpClient.GetFromJsonAsync<List<StoryPublishedModel>>(
                $"{api}/article/latest?count=18");

            if (latest == null)
                return Problem("Could not create a sitemap at this moment", null, 500, "Server Error");

            foreach (var article in latest)
            {
                var node = BuildNewsNode(article, smallCDN, bigCDN);
                if (node != null) nodes.Add(node);
            }

            // Fetch remaining articles — URL + dates only, no news metadata
            var rest = await httpClient.GetFromJsonAsync<List<StoryPublishedModel>>(
                $"{api}/article/latest?count=3000&offset=18");

            if (rest == null)
                return Problem("Could not create a sitemap at this moment", null, 500, "Server Error");

            foreach (var article in rest)
            {
                var node = BuildBasicNode(article);
                if (node != null) nodes.Add(node);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("SitemapController Exception: {0}", ex.Message);
            return View(null);
        }

        return _sitemapProvider.CreateSitemap(new SitemapModel(nodes));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private SitemapNode? BuildNewsNode(StoryPublishedModel article, string smallCDN, string bigCDN)
    {
        if (article.SlugTitle == null) return null;

        var title      = ResolveTitle(article);
        var pubDate    = ParseTimestamp(article.Timestamp);
        var langCode   = string.IsNullOrEmpty(article.LangCode) ? "eng" : article.LangCode;
        var imageUrl   = LinkUtil.GetDefaultImageLinkFromImageInfoMetadata(article.StylingInfo?.TitleBackgroundImage, smallCDN, bigCDN);

        var node = new SitemapNode(Url.Action("Index", "Story", new { slugTitle = article.SlugTitle }))
        {
            News = new SitemapNews(
                newsPublication: new NewsPublication(name: PublicationName, language: langCode),
                publicationDate: pubDate,
                title: title),
            ChangeFrequency = ChangeFrequency.Monthly,
            LastModificationDate = pubDate
        };

        if (imageUrl != null)
            node.Images = new List<SitemapImage> { new SitemapImage(imageUrl) };

        return node;
    }

    private SitemapNode? BuildBasicNode(StoryPublishedModel article)
    {
        if (article.SlugTitle == null) return null;

        return new SitemapNode(Url.Action("Index", "Story", new { slugTitle = article.SlugTitle }))
        {
            ChangeFrequency = ChangeFrequency.Monthly,
            LastModificationDate = ParseTimestamp(article.Timestamp)
        };
    }

    private static string ResolveTitle(StoryPublishedModel article) =>
        (!string.IsNullOrEmpty(article.StoryTitle)) ? article.StoryTitle! : article.EmptyTitle ?? "";

    private static DateTime ParseTimestamp(string? timestamp) =>
        string.IsNullOrEmpty(timestamp)
            ? DateTime.UtcNow
            : DateTime.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
}