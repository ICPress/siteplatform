using System.Text.Json;

namespace siteplatform.Models;

public sealed class InstrumentModel
{
    public string Id { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? TradingViewSymbol { get; set; }
    public bool HasSecFilings { get; set; }
    public string? GroupName { get; set; }
    public string? ReportingCurrency { get; set; }

    public List<InstrumentLevelModel> PivotLevels { get; set; } = [];
    public List<InstrumentGexModel> FutureGex { get; set; } = [];
    public InstrumentSnapshotModel Snapshot { get; set; } = new();
    public InstrumentRocModel Roc { get; set; } = new();
    public bool HasPineScript { get; set; }
    public bool HasPineLabels { get; set; }
    public string? PineScriptDate { get; set; }
    public PineScriptsModel? PineScripts { get; set; }
    public bool BetaJoined { get; set; }
    public bool BetaEnabled { get; set; }
    public bool ViewLimitExceeded { get; set; }
    public int InstrumentViewsUsed { get; set; }
    public string? InstrumentViewRetryAtUtc { get; set; }
    public bool HasLatestPriceLevels { get; set; }
    public bool HasFutureGex { get; set; }
    public InstrumentPatternSummaryModel PatternPreview { get; set; } = new();
    public InstrumentPatternSummaryModel? PatternDetails { get; set; }
    public AvailablePeriodsModel AvailablePeriods { get; set; } = new();
    public JsonElement? CurrentYearFundamentals { get; set; }

    public bool HasPriceStructureAnalysis => HasPineScript && (HasLatestPriceLevels || PivotLevels.Count > 0);
    public bool ShowResearchPack => HasSecFilings || HasPriceStructureAnalysis;
    public bool ShowGexSection => HasFutureGex || FutureGex.Count > 0;
}

public sealed class InstrumentLevelModel
{
    public string LevelName { get; set; } = "";
    public string LevelType { get; set; } = "TECHNICAL";
    public decimal? FixedPrice { get; set; }
    public decimal? RangeStart { get; set; }
    public decimal? RangeEnd { get; set; }
    public string? Formula { get; set; }
    public string Description { get; set; } = "";
    public string? AnalysisDate { get; set; }

    public decimal? Midpoint => FixedPrice ?? (RangeStart.HasValue && RangeEnd.HasValue
        ? (RangeStart.Value + RangeEnd.Value) / 2m : RangeStart ?? RangeEnd);
}

public sealed class InstrumentGexModel
{
    public string ExpirationDate { get; set; } = "";
    public string ExpirationType { get; set; } = "";
    public decimal? NetExposure { get; set; }
    public decimal? CallExposure { get; set; }
    public decimal? PutExposure { get; set; }
    public decimal? CallWallPrice { get; set; }
    public decimal? PutWallPrice { get; set; }
    public decimal? GammaFlipPrice { get; set; }
    public string? GammaFlipMethod { get; set; }
    public string? AnalysisDate { get; set; }
}

public sealed class InstrumentRocModel
{
    public decimal? AverageAbsoluteDailyRoc { get; set; }
    public int SampleSize { get; set; }
    public string? LatestCloseDate { get; set; }
    public decimal? WeekEnvelopePct { get; set; }
    public decimal? MonthEnvelopePct { get; set; }
    public decimal? YearEnvelopePct { get; set; }
}

public sealed class FinancialMetricsResponse
{
    public string SelectionMode { get; set; } = "";
    public List<FinancialMetricRow> Rows { get; set; } = [];
}
public sealed class FinancialMetricRow
{
    public string MetricKey { get; set; } = "";
    public string MetricName { get; set; } = "";
    public JsonElement MetricValue { get; set; }
    public string UnitCode { get; set; } = "";
    public string FormType { get; set; } = "";
    public string PeriodType { get; set; } = "";
    public int FiscalYear { get; set; }
    public int? FiscalQuarter { get; set; }
    public string PeriodEndDate { get; set; } = "";
    public string AccessionNumber { get; set; } = "";
    public bool IsGaap { get; set; }
    public bool Verified { get; set; }
}

public sealed class InstrumentSnapshotModel
{
    public string? LatestCloseDate { get; set; }
    public decimal? OpenPrice { get; set; }
    public decimal? ClosePrice { get; set; }
    public decimal? PreviousClose { get; set; }
}

public sealed class PineStatusModel
{
    public bool HasPineScript { get; set; }
    public bool HasLabels { get; set; }
    public bool HasPriceLevels { get; set; }
    public string? CreatedDate { get; set; }
}

public sealed class InstrumentPatternSummaryModel
{
    public bool HasLatestScript { get; set; }
    public bool HasFutureGex { get; set; }
    public string? GexExpirationDate { get; set; }
    public int TotalCount { get; set; }
    public List<PricePatternItemModel> Patterns { get; set; } = [];
}

public class PricePatternItemModel
{
    public string Key { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Tone { get; set; } = "neutral";
    public bool IsGexDerived { get; set; }
}

public sealed class PricePatternScannerModel
{
    public int TotalIdentifiedPatterns { get; set; }
    public int TotalInstruments { get; set; }
    public List<PricePatternCountModel> Patterns { get; set; } = [];
}

public sealed class PricePatternCountModel : PricePatternItemModel
{
    public int Count { get; set; }
}

public sealed class PricePatternInstrumentsModel
{
    public PricePatternItemModel Pattern { get; set; } = new();
    public int Count { get; set; }
    public List<PricePatternInstrumentModel> Instruments { get; set; } = [];
}

public sealed class PricePatternInstrumentModel
{
    public string Symbol { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal? LatestClose { get; set; }
    public string? LatestCloseDate { get; set; }
}

public sealed class PineScriptsModel
{
    public long PineScriptId { get; set; }
    public string CreatedDate { get; set; } = "";
    public string StructureScript { get; set; } = "";
    public string? LabelScript { get; set; }
}

public sealed class AvailablePeriodsModel
{
    public int[] Years { get; set; } = [];
    public List<AvailableQuarterGroupModel> Quarters { get; set; } = [];
}

public sealed class AvailableQuarterGroupModel
{
    public int Year { get; set; }
    public int[] AvailableQuarters { get; set; } = [];
}

public sealed class InstrumentViewAccessModel
{
    public bool Found { get; set; }
    public bool Allowed { get; set; }
    public bool BetaEnabled { get; set; }
    public int ViewsUsed { get; set; }
    public int ViewLimit { get; set; }
    public DateTime? RetryAtUtc { get; set; }
}

public sealed class BetaWaitlistStatusModel
{
    public bool Joined { get; set; }
    public bool BetaEnabled { get; set; }
    public string? ContactConsentAt { get; set; }
}

public sealed class MarketPageModel
{
    public string CountryCode { get; set; } = "US";
    public string? LatestCloseDate { get; set; }
    public string? SelectedGroup { get; set; }
    public int GreenCount { get; set; }
    public int RedCount { get; set; }
    public List<MarketGroupModel> Groups { get; set; } = [];
}

public sealed class MarketGroupModel
{
    public string Name { get; set; } = "";
    public int GreenCount { get; set; }
    public int RedCount { get; set; }
    public List<MarketInstrumentModel> Instruments { get; set; } = [];
}

public sealed class MarketInstrumentModel
{
    public string Id { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string CountryCode { get; set; } = "US";
    public string Name { get; set; } = "";
    public string LatestCloseDate { get; set; } = "";
    public decimal OpenPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal? PreviousClose { get; set; }
    public string Direction { get; set; } = "flat";
}

public sealed class WebAccountModel
{
    public string? AvatarUrl { get; set; }
    public string Username { get; set; } = "";
    public string? FullName { get; set; }
    public string Email { get; set; } = "";
    public bool OnBetaWaitingList { get; set; }
    public long UnreadNotifications { get; set; }
    public long UnreadMessages { get; set; }
}


public sealed class PricePatternPageModel
{
    public PricePatternScannerModel Scanner { get; set; } = new();
    public PricePatternInstrumentsModel? SelectedPattern { get; set; }
}
