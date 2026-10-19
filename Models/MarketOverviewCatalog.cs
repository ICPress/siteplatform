namespace siteplatform.Models;

// The prototype's fixed ETF selector is independent of database coverage.
public static class MarketOverviewCatalog
{
    public static IReadOnlyList<InstrumentModel> All { get; } = new InstrumentModel[]
    {
        new() { Id = "etf/SPY", Symbol = "SPY", Name = "S&P 500 Index", Exchange = "AMEX", CountryCode = "US", Type = "etf", TradingViewSymbol = "AMEX:SPY" },
        new() { Id = "etf/QQQ", Symbol = "QQQ", Name = "Nasdaq 100 Index", Exchange = "NASDAQ", CountryCode = "US", Type = "etf", TradingViewSymbol = "NASDAQ:QQQ" },
        new() { Id = "etf/DIA", Symbol = "DIA", Name = "Dow Jones Industrial Average", Exchange = "AMEX", CountryCode = "US", Type = "etf", TradingViewSymbol = "AMEX:DIA" },
        new() { Id = "etf/IWM", Symbol = "IWM", Name = "Russell 2000 Index", Exchange = "AMEX", CountryCode = "US", Type = "etf", TradingViewSymbol = "AMEX:IWM" },
        new() { Id = "etf/SMH", Symbol = "SMH", Name = "VanEck Semiconductor ETF", Exchange = "NASDAQ", CountryCode = "US", Type = "etf", TradingViewSymbol = "NASDAQ:SMH" },
        new() { Id = "etf/XLK", Symbol = "XLK", Name = "Technology Select Sector", Exchange = "AMEX", CountryCode = "US", Type = "etf", TradingViewSymbol = "AMEX:XLK" },
        new() { Id = "etf/IBIT", Symbol = "IBIT", Name = "iShares Bitcoin Trust", Exchange = "NASDAQ", CountryCode = "US", Type = "etf", TradingViewSymbol = "NASDAQ:IBIT" },
        new() { Id = "etf/GLD", Symbol = "GLD", Name = "SPDR Gold Shares", Exchange = "AMEX", CountryCode = "US", Type = "etf", TradingViewSymbol = "AMEX:GLD" },
        new() { Id = "etf/TLT", Symbol = "TLT", Name = "20+ Year Treasury Bond", Exchange = "NASDAQ", CountryCode = "US", Type = "etf", TradingViewSymbol = "NASDAQ:TLT" },
        new() { Id = "etf/USO", Symbol = "USO", Name = "United States Oil Fund", Exchange = "AMEX", CountryCode = "US", Type = "etf", TradingViewSymbol = "AMEX:USO" },
        new() { Id = "etf/EEM", Symbol = "EEM", Name = "MSCI Emerging Markets", Exchange = "AMEX", CountryCode = "US", Type = "etf", TradingViewSymbol = "AMEX:EEM" },
        new() { Id = "etf/VGK", Symbol = "VGK", Name = "FTSE Europe ETF", Exchange = "AMEX", CountryCode = "US", Type = "etf", TradingViewSymbol = "AMEX:VGK" },
    };
}
