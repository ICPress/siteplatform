using System.Text.Json;
 
namespace siteplatform.Util;
 
public static class LinkUtil
{
    public static string GetLinkPathFromModel(PageModel? model)
    {
        if (model?.Query == null)
            return "/p";
 
        var queryReplaced = model.Query.Replace("@", "").Replace("#", "");
 
        if (model.Query.Contains('@'))
            return "/q/u/" + queryReplaced;
 
        return "/q/" + queryReplaced;
    }
 
    /// <summary>
    /// Resolves a CDN image URL from a raw JSON ImageInfoMetadata string.
    /// Parameter order: bigCDN first, smallCDN second — matches all call sites.
    /// </summary>
    public static string? GetDefaultImageLinkFromImageInfoMetadata(
        string? imageInfoMetadata, string bigCDN, string smallCDN)
    {
        if (imageInfoMetadata == null) return null;
        try
        {
            var meta = JsonSerializer.Deserialize<ImageInfoMetadata>(imageInfoMetadata);
            return GetDefaultImageLinkFromImageInfoMetadataParsed(meta, bigCDN, smallCDN);
        }
        catch
        {
            return null;
        }
    }
 
    /// <summary>
    /// Resolves a CDN image URL from a parsed ImageInfoMetadata object.
    /// Uses smallCDN + "m_" prefix when a miniature width is present, bigCDN otherwise.
    /// </summary>
    public static string? GetDefaultImageLinkFromImageInfoMetadataParsed(
        ImageInfoMetadata? backgroundImageMetadata, string bigCDN, string smallCDN)
    {
        if (backgroundImageMetadata == null) return null;
 
        if (backgroundImageMetadata.MinWidth != null)
            return smallCDN + "m_" + backgroundImageMetadata.Name;
 
        return bigCDN + backgroundImageMetadata.Name;
    }
 
    /// <summary>
    /// Returns the smallest available image URL for card thumbnail preloads.
    /// Prefers smallCDN + "m_" prefix when a miniature exists,
    /// falls back to bigCDN full image. Returns null when unparseable.
    /// </summary>
    public static string? GetMiniatureUrl(
        string? imageInfoMetadata, string bigCDN, string smallCDN)
    {
        if (imageInfoMetadata == null) return null;
        try
        {
            var meta = JsonSerializer.Deserialize<ImageInfoMetadata>(imageInfoMetadata);
            if (meta?.Name == null) return null;
            return meta.MinWidth != null
                ? smallCDN + "m_" + meta.Name
                : bigCDN   + meta.Name;
        }
        catch { return null; }
    }
 
    /// <summary>
    /// Returns both CDN URLs needed to build a proper srcset for the LCP hero image.
    /// When a miniature exists: smallCDN/m_name (200w) + bigCDN/name (full resolution).
    /// When no miniature: bigCDN/name only — srcset/sizes are null.
    /// Returns null when imageInfoMetadata is null or unparseable.
    /// </summary>
    public static HeroImageUrls? GetHeroImageUrls(
        string? imageInfoMetadata, string bigCDN, string smallCDN)
    {
        if (imageInfoMetadata == null) return null;
        try
        {
            var meta = JsonSerializer.Deserialize<ImageInfoMetadata>(imageInfoMetadata);
            if (meta?.Name == null) return null;
 
            var bigUrl = bigCDN + meta.Name;
 
            if (meta.MinWidth != null)
            {
                var smallUrl = smallCDN + "m_" + meta.Name;
                return new HeroImageUrls
                {
                    Src    = bigUrl,
                    SrcSet = $"{smallUrl} 200w, {bigUrl}",
                    Sizes  = "(max-width: 600px) 200px, 100vw",
                    Width  = meta.Width,
                    Height = meta.Height
                };
            }
 
            return new HeroImageUrls
            {
                Src    = bigUrl,
                SrcSet = null,
                Sizes  = null,
                Width  = meta.Width,
                Height = meta.Height
            };
        }
        catch
        {
            return null;
        }
    }
}
 
public class HeroImageUrls
{
    public string  Src    { get; set; } = "";
    public string? SrcSet { get; set; }
    public string? Sizes  { get; set; }
    public int?    Width  { get; set; }
    public int?    Height { get; set; }
}