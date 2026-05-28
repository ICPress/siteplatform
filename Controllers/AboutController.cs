using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;
using siteplatform.Models;

namespace siteplatform.Controllers;

[Route("about")]
public class AboutController : Controller
{
    private readonly ServerSettings _serverSettings;

    public AboutController(IOptions<ServerSettings> serverSettings)
    {
        _serverSettings = serverSettings.Value;
    }

    [ResponseCache(Duration = 3600)]
    [HttpGet()]
    public IActionResult Index()
    {
        ViewData["Title"] = "About ICPress — Our Mission & Team";
        return View();
    }

    [ResponseCache(Duration = 3600)]
    [HttpGet("guidelines")]
    public async Task<IActionResult> Guidelines()
    {
        ViewData["Title"] = "Editorial Guidelines — ICPress";
        using var httpClient = new HttpClient();
        var guidelines = await httpClient.GetFromJsonAsync<ArticleGuidelines>(
            $"{_serverSettings.PublicApiUrl}/article/guidelines");

        return View(guidelines);
    }
}
