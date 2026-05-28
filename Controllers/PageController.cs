using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace siteplatform.Controllers;

[Route("p")]
public class PageController : Controller
{
    private readonly ILogger<PageController> _logger;
    private readonly ServerSettings _serverSettings;

    public PageController(ILogger<PageController> logger, IOptions<ServerSettings> serverSettings)
    {
        _logger = logger;
        _serverSettings = serverSettings.Value;
    }

    [ResponseCache(Duration = 120)]
    [HttpGet("{page}")]
    public async Task<IActionResult> Index(int page)
    {
        try
        {
            page = page - 1;
            const int requestCount = 18;

            using var httpClient = new HttpClient();
            var articles = await httpClient.GetFromJsonAsync<List<StoryPublishedModel>>(
                $"{_serverSettings.PublicApiUrl}/article/latest?count={requestCount}&offset={page * requestCount}");

            var pagesLess = Enumerable.Range(Math.Max(1, page + 1 - 4), 5).ToArray();
            var pagesMore = Enumerable.Range(page + 1, 5).ToArray();
            var pageModel = new PageModel(articles, page + 1, pagesLess.Union(pagesMore).ToArray(), requestCount);

            return View("Browse",pageModel);
        }
        catch (Exception ex)
        {
            _logger.LogError("ArticlesFetch Page Exception: {0}", ex.Message);
            return View(null);
        }
    }
}