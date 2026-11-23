using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;
using siteplatform.Services;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Platform.Instruments;

namespace siteplatform.Controllers;

[Route("instrument")]
public class InstrumentController(PlatformApi api, IWebHostEnvironment environment) : Controller
{
    private string? Token => User.FindFirstValue("api_token");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    [HttpGet("{country}/{exchange}/{symbol}")]
    [ResponseCache(Duration = 120, VaryByHeader = "Cookie")]
    public async Task<IActionResult> Index(string country, string exchange, string symbol)
    {
        var model = await LoadDashboardModel(country, exchange, symbol);
        if (model == null) return NotFound();
        if (User.Identity?.IsAuthenticated == true) Response.Headers.CacheControl = "private, no-cache";
        return View(model);
    }

    [HttpGet("search")]
    public Task<IActionResult> Search(string? q) => Proxy("Instrument/search?q=" + Uri.EscapeDataString(q ?? ""));

    [HttpGet("overview")]
    public IActionResult Overview() => Json(MarketOverviewCatalog.All);

    [HttpGet("{country}/{exchange}/{symbol}/fundamentals")]
    public async Task<IActionResult> Fundamentals(string country, string exchange, string symbol,
        int? year = null, int? quarter = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (year.HasValue) query.Add("year=" + year.Value);
        if (quarter.HasValue) query.Add("quarter=" + quarter.Value);
        var suffix = query.Count == 0 ? "" : "?" + string.Join("&", query);
        using var response = await api.SendAsync(HttpMethod.Get,
            $"Instrument/fundamentals/{Uri.EscapeDataString(country)}/{Uri.EscapeDataString(exchange)}/{Uri.EscapeDataString(symbol)}{suffix}",
            token: Token);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = await response.Content.ReadAsStringAsync(cancellationToken),
            ContentType = "application/json"
        };
    }

    [Authorize]
    [HttpGet("{country}/{exchange}/{symbol}/bars")]

    public async Task<IActionResult> Bars(string country, string exchange, string symbol,
        string? before = null, string timeframe = "1D", int limit = 300,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Token)) return Unauthorized();
        var query = $"?timeframe={Uri.EscapeDataString(timeframe)}&limit={limit}" +
            (string.IsNullOrWhiteSpace(before) ? "" : "&before=" + Uri.EscapeDataString(before));
        using var response = await api.SendAsync(HttpMethod.Get,
            $"Market/bars/{Uri.EscapeDataString(country)}/{Uri.EscapeDataString(exchange)}/{Uri.EscapeDataString(symbol)}{query}", token: Token);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = await response.Content.ReadAsStringAsync(cancellationToken),
            ContentType = "application/json"
        };
    }

    [Authorize]
    [HttpGet("{country}/{exchange}/{symbol}/pine-scripts")]
    public async Task<IActionResult> PineScripts(string country, string exchange, string symbol)
    {
        using var response = await api.SendAsync(HttpMethod.Get,
            $"Instrument/pine-scripts/{Uri.EscapeDataString(country)}/{Uri.EscapeDataString(exchange)}/{Uri.EscapeDataString(symbol)}", token: Token);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = await response.Content.ReadAsStringAsync(),
            ContentType = "application/json"
        };
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("{country}/{exchange}/{symbol}/beta-waitlist")]
    public async Task<IActionResult> JoinBetaWaitlist(string country, string exchange, string symbol)
    {
        using var response = await api.SendAsync(HttpMethod.Post, "Instrument/beta-waitlist",
            new { acceptContact = true }, Token);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = await response.Content.ReadAsStringAsync(),
            ContentType = "application/json"
        };
    }

    // Cookie authentication protects the ZIP itself, not merely the button.
    [Authorize]
    [HttpGet("{country}/{exchange}/{symbol}/research-pack")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DownloadResearchPack(string country, string exchange, string symbol,
        CancellationToken cancellationToken)
    {
        var key = $"{Uri.EscapeDataString(country)}/{Uri.EscapeDataString(exchange)}/{Uri.EscapeDataString(symbol)}";
        var access = await GetApiAsync<InstrumentViewAccessModel>($"Instrument/view-access/{key}", Token);
        if (access == null) return StatusCode(503, "Instrument access could not be verified.");
        if (!access.Found) return Unauthorized();
        if (!access.Allowed)
            return StatusCode(429, "Due to heavy load, access is limited to five instruments per user every 24 hours for accounts not enrolled in the beta. Join the beta waitlist to request unlimited access.");
        var instrument = await GetApiAsync<InstrumentModel>($"Instrument/{key}");
        if (instrument == null) return NotFound();
        var levels = await GetApiAsync<List<InstrumentLevelModel>>($"Instrument/pivots/{key}", Token);
        var gex = await GetApiAsync<List<InstrumentGexModel>>($"Instrument/gex/{key}", Token);
        var structure = await GetApiAsync<PineStatusModel>($"Instrument/pine-status/{key}");
        if (levels == null || gex == null || structure == null)
            return StatusCode(503, "Research data is temporarily unavailable.");
        // Match the public dashboard: a non-filing instrument requires both a
        // current structure analysis and its associated levels to offer a pack.
        if (!instrument.HasSecFilings && (structure.HasPineScript != true || levels.Count == 0))
            return NotFound("Price structure analysis is ongoing.");

        FinancialMetricsResponse? fundamentals = null;
        if (instrument.HasSecFilings)
        {
            fundamentals = await GetApiAsync<FinancialMetricsResponse>($"Instrument/fundamentals/{key}", Token);
            if (fundamentals == null) return StatusCode(503, "Financial metrics are temporarily unavailable.");
        }

        static string DecimalText(decimal? n) => n.HasValue
            ? decimal.Round(n.Value, 3, MidpointRounding.AwayFromZero).ToString("0.000", CultureInfo.InvariantCulture)
            : "";
        static string MetricValueText(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return decimal.Round(number, 3, MidpointRounding.AwayFromZero).ToString("0.000", CultureInfo.InvariantCulture);
            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var parsed))
                return decimal.Round(parsed, 3, MidpointRounding.AwayFromZero).ToString("0.000", CultureInfo.InvariantCulture);
            return value.ToString();
        }

        var hasTechnicalData = structure.HasPineScript && levels.Count > 0;
        var technicalRows = new List<string[]>();
        if (hasTechnicalData)
        {
            foreach (var level in levels)
            {
                var start = level.RangeStart ?? level.FixedPrice ?? level.RangeEnd;
                var end = level.RangeEnd ?? level.FixedPrice ?? level.RangeStart;
                technicalRows.Add([level.LevelName, level.LevelType, DecimalText(start), DecimalText(end),
                    level.Formula ?? "", level.Description]);
            }
            foreach (var item in gex)
            {
                void Include(string name, decimal? price, string? formula = null)
                {
                    if (!price.HasValue) return;
                    technicalRows.Add([$"GEX {name} · {item.ExpirationDate}", "GEX", DecimalText(price), DecimalText(price),
                        formula ?? "", $"Options-derived {name}; expiration {item.ExpirationDate}."]);
                }
                Include("put wall", item.PutWallPrice);
                Include("call wall", item.CallWallPrice);
                Include("gamma flip", item.GammaFlipPrice, item.GammaFlipMethod);
            }
        }
        var safeTickerPrefix = System.Text.RegularExpressions.Regex.Replace(instrument.Symbol, @"[^A-Za-z0-9._-]", "_") + "_";
        var technicalsTsv = hasTechnicalData
            ? BuildTsv(["level_name", "level_type", "range_start", "range_end", "formula", "description"], technicalRows)
            : null;

        await using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (instrument.HasSecFilings)
            {
                var guidePath = Path.Combine(environment.ContentRootPath, "ResearchPack", "SEC_FINANCIAL_METRICS_CALCULATION_GUIDE.md");
                if (!System.IO.File.Exists(guidePath)) return StatusCode(500, "The SEC calculation guide is missing.");
                var guide = await System.IO.File.ReadAllTextAsync(guidePath, cancellationToken);
                await AddTextEntryAsync(archive, "SEC_FINANCIAL_METRICS_CALCULATION_GUIDE.md", guide, cancellationToken);
                var metricRows = fundamentals!.Rows.Select(row => new[]
                {
                    row.MetricName, MetricValueText(row.MetricValue), row.UnitCode,
                    row.FormType, row.PeriodType, row.FiscalYear.ToString(CultureInfo.InvariantCulture),
                    row.FiscalQuarter?.ToString(CultureInfo.InvariantCulture) ?? "", row.PeriodEndDate,
                    row.AccessionNumber
                });
                await AddTextEntryAsync(archive, safeTickerPrefix + "financial_metrics.tsv", BuildTsv(
                    ["metric_name", "metric_value", "unit_code", "form_type", "period_type",
                     "fiscal_year", "fiscal_quarter", "period_end_date", "accession_number"],
                    metricRows), cancellationToken);
            }
            if (hasTechnicalData && technicalsTsv != null)
                await AddTextEntryAsync(archive, safeTickerPrefix + "technicals.tsv", technicalsTsv, cancellationToken);
        }
        return File(output.ToArray(), "application/zip", $"{instrument.Symbol}_AI_Ready_Research_Pack.zip");
    }

    // Quote tabs, CR/LF and literal quotation marks so spreadsheet readers
    // cannot split long, multi-line level descriptions into stray rows.
    private static string BuildTsv(IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        static string EscapeCell(string? field)
        {
            field ??= "";
            // Avoid formula execution when a research pack is opened in a spreadsheet.
            if (field.Length > 0 && "=+-@".Contains(field[0]) &&
                !System.Text.RegularExpressions.Regex.IsMatch(field,
                    @"^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?$"))
                field = "'" + field;
            return field.IndexOfAny(['\t', '\n', '\r', '"']) >= 0
                ? "\"" + field.Replace("\"", "\"\"") + "\"" : field;
        }
        var lines = new StringBuilder();
        lines.AppendLine(string.Join('\t', headers.Select(EscapeCell)));
        foreach (var row in rows) lines.AppendLine(string.Join('\t', row.Select(EscapeCell)));
        return lines.ToString();
    }

    private async Task<InstrumentModel?> LoadDashboardModel(string country, string exchange, string symbol)
    {
        var path = $"Instrument/{Uri.EscapeDataString(country)}/{Uri.EscapeDataString(exchange)}/{Uri.EscapeDataString(symbol)}";
        var model = await GetApiAsync<InstrumentModel>(path);
        if (model == null) return null;
        if (string.IsNullOrWhiteSpace(model.TradingViewSymbol))
            model.TradingViewSymbol = TradingViewSymbols.Resolve(model.Symbol, model.Exchange, model.CountryCode);

        var key = $"{Uri.EscapeDataString(country)}/{Uri.EscapeDataString(exchange)}/{Uri.EscapeDataString(symbol)}";
        var isAuthenticated = User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(Token);
        var previewQuery = "Instrument/pattern?country=" + Uri.EscapeDataString(country) +
            "&market=" + Uri.EscapeDataString(exchange) + "&symbol=" + Uri.EscapeDataString(symbol) + "&preview=true";

        var snapshotTask = GetApiAsync<InstrumentSnapshotModel>($"Instrument/snapshot/{key}");
        var rocTask = GetApiAsync<InstrumentRocModel>($"Instrument/roc/{key}");
        var pineStatusTask = GetApiAsync<PineStatusModel>($"Instrument/pine-status/{key}");
        var patternPreviewTask = GetApiAsync<InstrumentPatternSummaryModel>(previewQuery);
        var accessTask = isAuthenticated
            ? GetApiAsync<InstrumentViewAccessModel>($"Instrument/view-access/{key}", Token)
            : Task.FromResult<InstrumentViewAccessModel?>(null);
        var betaStatusTask = isAuthenticated
            ? GetApiAsync<BetaWaitlistStatusModel>("Instrument/beta-waitlist/status", Token)
            : Task.FromResult<BetaWaitlistStatusModel?>(null);

        await Task.WhenAll(snapshotTask, rocTask, pineStatusTask, patternPreviewTask, accessTask, betaStatusTask);

        model.Snapshot = snapshotTask.Result ?? new InstrumentSnapshotModel();
        model.Roc = rocTask.Result ?? new InstrumentRocModel();
        var pineStatus = pineStatusTask.Result ?? new PineStatusModel();
        model.HasPineScript = pineStatus.HasPineScript;
        model.HasPineLabels = pineStatus.HasLabels;
        model.HasLatestPriceLevels = pineStatus.HasPriceLevels;
        model.PineScriptDate = pineStatus.CreatedDate;
        model.PatternPreview = patternPreviewTask.Result ?? new InstrumentPatternSummaryModel();
        model.HasFutureGex = model.PatternPreview.HasFutureGex;

        var access = accessTask.Result;
        model.BetaJoined = betaStatusTask.Result?.Joined == true;
        model.BetaEnabled = access?.BetaEnabled == true || betaStatusTask.Result?.BetaEnabled == true;
        model.ViewLimitExceeded = isAuthenticated && !model.BetaEnabled && (access == null || !access.Allowed);
        model.InstrumentViewsUsed = access?.ViewsUsed ?? 0;
        model.InstrumentViewRetryAtUtc = access?.RetryAtUtc?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        // Public visitors still receive the blurred preview data. Authenticated
        // non-beta users only receive it while their rolling instrument allowance
        // is available; API endpoints independently enforce the same allowance.
        if (!model.ViewLimitExceeded)
        {
            var apiToken = isAuthenticated ? Token : null;
            var pivotTask = isAuthenticated
                ? GetApiAsync<List<InstrumentLevelModel>>($"Instrument/pivots/{key}", apiToken)
                : Task.FromResult<List<InstrumentLevelModel>?>(null);
            var gexTask = isAuthenticated
                ? GetApiAsync<List<InstrumentGexModel>>($"Instrument/gex/{key}", apiToken)
                : Task.FromResult<List<InstrumentGexModel>?>(null);
            var periodsTask = isAuthenticated && model.HasSecFilings
                ? GetApiAsync<AvailablePeriodsModel>($"Instrument/periods/{key}", apiToken)
                : Task.FromResult<AvailablePeriodsModel?>(new AvailablePeriodsModel());
            var patternDetailsTask = isAuthenticated
                ? GetApiAsync<InstrumentPatternSummaryModel>(previewQuery.Replace("&preview=true", ""), Token)
                : Task.FromResult<InstrumentPatternSummaryModel?>(null);
            var scriptsTask = isAuthenticated && model.HasPineScript
                ? GetApiAsync<PineScriptsModel>($"Instrument/pine-scripts/{key}", Token)
                : Task.FromResult<PineScriptsModel?>(null);

            await Task.WhenAll(pivotTask, gexTask, periodsTask, patternDetailsTask, scriptsTask);
            model.PivotLevels = pivotTask.Result ?? [];
            model.FutureGex = gexTask.Result ?? [];
            model.HasFutureGex = model.HasFutureGex || model.FutureGex.Count > 0;
            model.AvailablePeriods = periodsTask.Result ?? new AvailablePeriodsModel();
            model.PatternDetails = patternDetailsTask.Result;
            model.PineScripts = scriptsTask.Result;
        }
        else
        {
            model.PivotLevels = [];
            model.FutureGex = [];
            model.AvailablePeriods = new AvailablePeriodsModel();
            model.PatternDetails = null;
            model.PineScripts = null;
        }

        return model;
    }

    private async Task<T?> GetApiAsync<T>(string path, string? token = null)
    {
        try
        {
            using var response = await api.SendAsync(HttpMethod.Get, path, token: token);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return default;
        }
    }

    private static async Task AddTextEntryAsync(ZipArchive archive, string name, string content,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
    }

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
