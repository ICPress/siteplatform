using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;
using siteplatform.Services;
using System.Security.Claims;

namespace siteplatform.Controllers;

[Route("scanner")]
public sealed class ScannerController(PlatformApi api) : Controller
{
    private string? Token => User.FindFirstValue("api_token");

    [HttpGet("price_patterns")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> PricePatterns()
    {
        var scanner = await GetApiAsync<PricePatternScannerModel>("Instrument/pattern");
        if (scanner == null) return StatusCode(503, "Price-pattern data is temporarily unavailable.");
        return View("PricePatterns", new PricePatternPageModel { Scanner = scanner });
    }

    [Authorize]
    [HttpGet("price_patterns/{slug}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> PricePattern(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 64) return NotFound();
        if (string.IsNullOrWhiteSpace(Token)) return Challenge();

        var scanner = await GetApiAsync<PricePatternScannerModel>("Instrument/pattern");
        var selected = await GetApiAsync<PricePatternInstrumentsModel>(
            "Instrument/pattern/" + Uri.EscapeDataString(slug), Token);
        if (scanner == null || selected == null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return View("PricePatterns", new PricePatternPageModel
        {
            Scanner = scanner,
            SelectedPattern = selected
        });
    }

    // Preserve old inbound links while making the canonical scanner route explicit.
    [HttpGet("/price_patterns")]
    public IActionResult LegacyIndex() => RedirectPermanent("/scanner/price_patterns");

    [Authorize]
    [HttpGet("/price_patterns/{slug}")]
    public IActionResult LegacyPattern(string slug) =>
        RedirectPermanent("/scanner/price_patterns/" + Uri.EscapeDataString(slug));

    private async Task<T?> GetApiAsync<T>(string path, string? token = null)
    {
        try
        {
            using var response = await api.SendAsync(HttpMethod.Get, path, token: token);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return default;
        }
    }
}
