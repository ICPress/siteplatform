using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace siteplatform.Controllers;

[Route("q")]
public class QueryController : Controller
{
    private readonly ILogger<QueryController> _logger;
    private readonly ServerSettings _serverSettings;

    public QueryController(ILogger<QueryController> logger, IOptions<ServerSettings> serverSettings)
    {
        _logger = logger;
        _serverSettings = serverSettings.Value;
    }

    [ResponseCache(Duration = 120)]
    [HttpGet("{query}")]
    public async Task<IActionResult> Index(string query)
    {
        return await GetQuery(false, query, 0);
    }

    [ResponseCache(Duration = 120)]
    [HttpGet("{query}/{page}")]
    public async Task<IActionResult> Page(string query, int page)
    {
        return await GetQuery(false, query, page - 1);
    }

    [ResponseCache(Duration = 120)]
    [HttpGet("u/{username}")]
    public async Task<IActionResult> UserSearch(string username)
    {
        return await GetQuery(true, username, 0);
    }

    [ResponseCache(Duration = 120)]
    [HttpGet("u/{username}/{page}")]
    public async Task<IActionResult> UserSearchPage(string username, int page)
    {
        return await GetQuery(true, username, page - 1);
    }

    private async Task<ViewResult> GetQuery(bool isUserSearch, string query, int page = 0)
    {
        var search = isUserSearch ? "article/author/" : "article/tag/";
        try
        {
            const int requestCount = 18;

            using var httpClient = new HttpClient();
            var articles = await httpClient.GetFromJsonAsync<List<StoryPublishedModel>>(
                $"{_serverSettings.PublicApiUrl}/{search}{query}?count={requestCount}&offset={page * requestCount}");

            var pagesLess = Enumerable.Range(Math.Max(1, page + 1 - 4), 5).ToArray();
            var pagesMore = Enumerable.Range(page + 1, 5).ToArray();
            var pageModel = new PageModel(articles, page + 1, pagesLess.Union(pagesMore).ToArray(), requestCount)
            {
                Query = (isUserSearch ? '@' : '#') + query
            };

            return View("Browse", pageModel);
        }
        catch (Exception ex)
        {
            _logger.LogError("ArticlesFetch Query Exception: {0}", ex.Message);
            return View(null);
        }
    }
}