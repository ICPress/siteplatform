using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;
using siteplatform.Services;

namespace siteplatform.Controllers;

public class MarketController(PlatformApi api) : Controller
{
    [HttpGet("/market/{country}/group/{groupName}")]
    public async Task<IActionResult> Group(string country, string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || groupName.Length > 160) return NotFound();
        return await Fetch(country, groupName);
    }

    [HttpGet("/market/{country}")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> Index(string country)
    {
        return await Fetch(country, null);
    }

    private async Task<IActionResult> Fetch(string country, string? groupName)
    {
        if (string.IsNullOrWhiteSpace(country) || country.Length != 2 || !country.All(char.IsLetter))
            return NotFound();
        var countryCode = country.ToUpperInvariant();
        try
        {
            var path = "Market/overview/" + Uri.EscapeDataString(countryCode) + (groupName == null ? "" : "?groupName=" + Uri.EscapeDataString(groupName));
            using var response = await api.SendAsync(HttpMethod.Get, path);
            if (!response.IsSuccessStatusCode) return StatusCode(503, "Market data is temporarily unavailable.");
            var model = await response.Content.ReadFromJsonAsync<MarketPageModel>() ?? new MarketPageModel();
            model.CountryCode = countryCode;
            if (groupName != null && model.Groups.Count == 0) return NotFound();
            return View("Index", model);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return StatusCode(503, "Market data is temporarily unavailable.");
        }
    }
}
