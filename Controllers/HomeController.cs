using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using siteplatform.Models;

namespace siteplatform.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ServerSettings _serverSettings;

    public HomeController(ILogger<HomeController> logger, IOptions<ServerSettings> serverSettings)
    {
        _logger = logger;
        _serverSettings = serverSettings.Value;
    }

    [ResponseCache(Duration = 120)]
    public async Task<IActionResult> Index()
    {
        var model = new HomePageModel();
        var api = _serverSettings.PublicApiUrl;

        try
        {
            using var httpClient = new HttpClient();

            // Parallel fetch: latest articles + trending tags + trending categories
            var latestTask      = httpClient.GetFromJsonAsync<List<StoryPublishedModel>>($"{api}/article/latest?count=18");
            var tagsTask        = httpClient.GetFromJsonAsync<List<TrendingTagModel>>($"{api}/tag/trending?n=12");
            var categoriesTask  = httpClient.GetFromJsonAsync<List<TrendingTagModel>>($"{api}/tag/trending/categories?n=8");

            await Task.WhenAll(latestTask, tagsTask, categoriesTask);

            model.LatestStories      = latestTask.Result      ?? new();
            model.TrendingTags       = tagsTask.Result        ?? new();
            model.TrendingCategories = categoriesTask.Result  ?? new();

            // Fetch one story strip per top category (up to 4 categories, 4 stories each)
            var topCategories = model.TrendingCategories.Take(4).ToList();
            var categoryTasks = topCategories.Select(c =>
                httpClient.GetFromJsonAsync<List<StoryPublishedModel>>(
                    $"{api}/article/category/{Uri.EscapeDataString(c.Tag)}?count=4")
            ).ToList();

            var categoryResults = await Task.WhenAll(categoryTasks);

            for (int i = 0; i < topCategories.Count; i++)
            {
                var stories = categoryResults[i];
                if (stories?.Any() == true)
                {
                    model.CategorySections.Add(new CategorySection
                    {
                        Category = topCategories[i].Tag,
                        Stories  = stories
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("HomeController Index Exception: {0}", ex.Message);
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}