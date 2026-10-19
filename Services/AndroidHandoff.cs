using System.Text.RegularExpressions;

namespace siteplatform.Services;

public static class AndroidHandoff
{
    public static string BuildIntent(string verificationUrl, string verificationToken, string returnUrl)
    {
        if (!Uri.TryCreate(verificationUrl, UriKind.Absolute, out var page) || page.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(page.UserInfo) || !string.IsNullOrEmpty(page.Query) || !string.IsNullOrEmpty(page.Fragment))
            throw new ArgumentException("AndroidVerificationUrl must be an HTTPS URL without a query or fragment.");
        if (verificationToken.Length > 4096 || !Regex.IsMatch(verificationToken, @"\A[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\z"))
            throw new ArgumentException("Invalid app verification token.");
        // Defence in depth: the caller also applies MVC's IsLocalUrl check.
        if (!returnUrl.StartsWith('/') || returnUrl.StartsWith("//") || returnUrl.Contains('\\')) returnUrl = "/";
        var fallback = new Uri(page, returnUrl).AbsoluteUri;
        // Preserve the original Android contract: HTTPS /account/verify?<raw JWT>.
        // The app exchanges this one-time proof through POST /Account/verify/{authToken}.
        return "intent://" + page.Authority + page.AbsolutePath + "?" + Uri.EscapeDataString(verificationToken) +
            "#Intent;scheme=https;package=net.crowdventures.storypop;S.browser_fallback_url=" +
            Uri.EscapeDataString(fallback) + ";end";
    }
}
