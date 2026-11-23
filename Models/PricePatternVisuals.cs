namespace siteplatform.Models;

/// <summary>Trusted, static SVG artwork for the fixed price-pattern catalog.</summary>
public static class PricePatternVisuals
{
    public static string Svg(string key)
    {
        var paths = key switch
        {
            "has_head_and_shoulders" => """<path class="icon-muted icon-dash" d="M4 19 H44"/><path class="icon-stroke" d="M2 19 L9 11 L16 19 L24 4 L32 19 L39 11 L46 19"/>""",
            "has_inverse_head_and_shoulders" => """<path class="icon-muted icon-dash" d="M4 9 H44"/><path class="icon-stroke" d="M2 9 L9 17 L16 9 L24 24 L32 9 L39 17 L46 9"/>""",
            "has_cup_and_handle" => """<path class="icon-stroke" d="M3 6 C7 25 33 25 37 6"/><path class="icon-stroke" d="M37 6 C39.5 12.5 43 13.5 46 8"/>""",
            "has_inverse_cup_and_handle" => """<path class="icon-stroke" d="M3 22 C7 3 33 3 37 22"/><path class="icon-stroke" d="M37 22 C39.5 15.5 43 14.5 46 20"/>""",
            "has_channel" => """<path class="icon-muted" d="M2 11 L46 3"/><path class="icon-muted" d="M2 23 L46 15"/><path class="icon-stroke" d="M5 21 L12 13 L20 18 L28 10 L36 15 L43 7"/>""",
            "has_wedge" => """<path class="icon-muted" d="M2 20 L46 8"/><path class="icon-muted" d="M2 26 L46 12"/><path class="icon-stroke" d="M5 23 L13 18 L21 19 L31 15 L39 12 L43 10"/>""",
            "has_flag" => """<path class="icon-stroke" d="M5 25 L11 8"/><path class="icon-stroke" d="M11 8 L28 12"/><path class="icon-stroke" d="M11 16 L28 20"/><path class="icon-muted icon-dash" d="M28 12 L28 20"/>""",
            "has_uptrend" => """<path class="icon-stroke" d="M3 23 L11 15 L17 19 L27 10 L33 14 L44 4"/><path class="icon-stroke" d="M38 4 L44 4 L44 10"/>""",
            "has_downtrend" => """<path class="icon-stroke" d="M3 5 L11 13 L17 9 L27 18 L33 14 L44 24"/><path class="icon-stroke" d="M38 24 L44 24 L44 18"/>""",
            "has_neutral_trend" => """<path class="icon-muted icon-dash" d="M2 14 H46"/><path class="icon-stroke" d="M3 14 L9 7 L16 21 L23 9 L30 20 L37 11 L44 15"/>""",
            "has_rsi_bullish_divergence" => """<path class="icon-stroke" d="M2 5 L10 12 L18 8 L30 15 L44 11"/><path class="icon-muted icon-dash" d="M2 19 H46"/><path class="icon-stroke icon-soft" d="M2 21 L10 25 L18 22 L30 23 L44 19"/>""",
            "has_rsi_bearish_divergence" => """<path class="icon-stroke" d="M2 15 L10 7 L18 12 L30 4 L44 9"/><path class="icon-muted icon-dash" d="M2 19 H46"/><path class="icon-stroke icon-soft" d="M2 24 L10 20 L18 23 L30 22 L44 25"/>""",
            "price_below_put_wall" => """<path class="icon-wall" d="M2 9 H46"/><path class="icon-stroke" d="M2 16 L10 20 L18 15 L26 21 L34 17 L46 22"/>""",
            "price_above_call_wall" => """<path class="icon-wall" d="M2 19 H46"/><path class="icon-stroke" d="M2 12 L10 8 L18 13 L26 7 L34 11 L46 6"/>""",
            _ => """<path class="icon-muted" d="M3 14 H45"/>"""
        };
        return $"""<svg viewBox="0 0 48 28" focusable="false" aria-hidden="true">{paths}</svg>""";
    }

    public static string SlugFor(string key) =>
        key switch
        {
            "has_head_and_shoulders" => "h&s",
            "has_inverse_head_and_shoulders" => "inverse-h&s",
            "has_cup_and_handle" => "cup-and-handle",
            "has_inverse_cup_and_handle" => "inverse-cup-and-handle",
            "has_channel" => "channel",
            "has_wedge" => "wedge",
            "has_flag" => "flag",
            "has_uptrend" => "uptrend",
            "has_downtrend" => "downtrend",
            "has_neutral_trend" => "neutral-trend",
            "has_rsi_bullish_divergence" => "rsi-bullish-divergence",
            "has_rsi_bearish_divergence" => "rsi-bearish-divergence",
            "price_below_put_wall" => "price-below-put-wall",
            "price_above_call_wall" => "price-above-call-wall",
            _ => key
        };
}
