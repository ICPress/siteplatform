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
    // Missing API flags and static catalog entries must not enable fair values.
    public bool HasSecFilings { get; set; }
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
