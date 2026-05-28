using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace siteplatform.Controllers;

[Route("s")]
public class StoryController : Controller
{
    private readonly ILogger<StoryController> _logger;
    private readonly ServerSettings _serverSettings;

    public StoryController(ILogger<StoryController> logger, IOptions<ServerSettings> serverSettings)
    {
        _logger = logger;
        _serverSettings = serverSettings.Value;
    }

    [ResponseCache(Duration = 520)]
    [HttpGet("{slugTitle}")]
    public async Task<IActionResult> Index(string slugTitle)
    {
        try
        {
            var api = _serverSettings.PublicApiUrl;
            using var httpClient = new HttpClient();

            // Fetch article and similar articles in parallel
            var articleTask = httpClient.GetFromJsonAsync<StoryPublishedModel>(
                $"{api}/article/title/{slugTitle}");
            var similarTask = httpClient.GetFromJsonAsync<List<StoryPublishedModel>>(
                $"{api}/article/similar/{slugTitle}?count=6");

            await Task.WhenAll(articleTask, similarTask);

            var model = new StoryPageModel
            {
                Article         = articleTask.Result,
                SimilarArticles = similarTask.Result ?? new List<StoryPublishedModel>()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError("Article Fetch Exception: {0}", ex.Message);
            return View(new StoryPageModel());
        }
    }
}