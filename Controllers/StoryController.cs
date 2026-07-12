using System.Text.Json;
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

            var articleResponseTask = httpClient.GetAsync(
                $"{api}/article/title/{slugTitle}");
            var similarTask = httpClient.GetFromJsonAsync<List<StoryPublishedModel>>(
                $"{api}/article/similar/{slugTitle}?count=6");

            await Task.WhenAll(articleResponseTask, similarTask);

            var articleResponse = articleResponseTask.Result;
            StoryPublishedModel? article = null;
            if (articleResponse.IsSuccessStatusCode)
            {
                var content = await articleResponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    article = JsonSerializer.Deserialize<StoryPublishedModel>(content);
                }
            }

            var model = new StoryPageModel
            {
                Article         = article,
                SimilarArticles = similarTask.Result ?? new List<StoryPublishedModel>()
            };
            
            if (model.Article == null && (articleResponse.StatusCode == System.Net.HttpStatusCode.NotFound || articleResponse.StatusCode == System.Net.HttpStatusCode.Gone))
            {
                Response.StatusCode =  (articleResponse.StatusCode == System.Net.HttpStatusCode.NotFound) ? StatusCodes.Status404NotFound : StatusCodes.Status410Gone;
                Response.Headers["Cache-Control"] = "no-store";
            } else if (model.Article == null) throw new InvalidOperationException($"Article could not be fetched, article: {slugTitle}, reason:{articleResponse.ReasonPhrase}");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError("Article Fetch Exception: {0}", ex.Message);
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            Response.Headers["Cache-Control"] = "no-store";
            return View(new StoryPageModel());
        }
    }
}