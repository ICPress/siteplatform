using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;
using siteplatform.Services;
using Platform.Instruments;
namespace siteplatform.Controllers;
[Route("instrument")]
public class InstrumentController(PlatformApi api) : Controller
{
    [HttpGet("{id:long}")]
    [ResponseCache(Duration = 120)]
    public async Task<IActionResult> Index(long id)
    {
        try
        {
            using var response = await api.SendAsync(HttpMethod.Get, $"Instrument/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
            if (!response.IsSuccessStatusCode) return StatusCode(503, "Instrument information is temporarily unavailable.");
            var model = await response.Content.ReadFromJsonAsync<InstrumentModel>();
            if (model == null) return NotFound();
            if (string.IsNullOrWhiteSpace(model.TradingViewSymbol))
                model.TradingViewSymbol = TradingViewSymbols.Resolve(model.Symbol, model.Exchange, model.CountryCode);
            return View(model);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        { return StatusCode(503, "Instrument information is temporarily unavailable."); }
    }

    [HttpGet("etf/{symbol}")]
    [ResponseCache(Duration = 120)]
    public IActionResult Etf(string symbol)
    {
        var model = MarketOverviewCatalog.All.FirstOrDefault(item =>
            string.Equals(item.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
        return model == null ? NotFound() : View("Index", model);
    }

    [HttpGet("search")]
    public Task<IActionResult> Search(string? q) => Proxy("Instrument/search?q=" + Uri.EscapeDataString(q ?? ""));
    [HttpGet("overview")]
    public IActionResult Overview() => Json(MarketOverviewCatalog.All);
    private async Task<IActionResult> Proxy(string path)
    {
        try
        {
            using var response = await api.SendAsync(HttpMethod.Get, path);
            if (!response.IsSuccessStatusCode) return StatusCode(503);
            return Content(await response.Content.ReadAsStringAsync(), "application/json");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { return StatusCode(503); }
    }
}
