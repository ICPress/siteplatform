using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;
using siteplatform.Services;
using System.Security.Claims;
using System.Text.Json;

namespace siteplatform.Controllers;

[Route("Account")]
public class AccountController(PlatformApi api, ServerSettings settings, ILogger<AccountController> logger) : Controller
{
    public const string OnboardingScheme = "Onboarding";

    [HttpGet("signin")]
    public IActionResult SignIn(string? returnUrl = null, string? method = null)
    {
        ViewBag.ReturnUrl = SafeReturn(returnUrl);
        ViewBag.GoogleClientId = settings.GoogleClientId;
        ViewBag.EmailStep = method == "email";
        return View("SignIn");
    }

    [EnableRateLimiting("auth")]
    [HttpPost("email")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Email(string email, string? returnUrl)
    {
        if (!System.Net.Mail.MailAddress.TryCreate(email, out _) || email.Length > 255)
            return SignInError("Please enter a valid email address.", returnUrl, "email");
        try
        {
            using var response = await api.SendAsync(HttpMethod.Post, "Account/web/email", new { email = email.Trim() });
            if (!response.IsSuccessStatusCode) return SignInError("We couldn’t send the link. Please try again shortly.", returnUrl, "email");
            TempData["LoginReturnUrl"] = SafeReturn(returnUrl);
            ViewBag.Message = "Check your inbox for your sign-in link. Links expire after 30 minutes. If you requested one recently, use that link.";
            return SignIn(returnUrl, "email");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        { return SignInError("Sign-in is temporarily unavailable. Please try again.", returnUrl); }
    }

    [HttpPost("profile-image/prepare",Name="RegistrationImagePrepare"),ValidateAntiForgeryToken,EnableRateLimiting("auth")]
    public async Task<IActionResult> PrepareProfileImage(int width,int height)
    {
        Response.Headers.CacheControl="no-store";
        var pending=await HttpContext.AuthenticateAsync(OnboardingScheme);var proof=pending.Principal?.FindFirstValue("registration_token");
        if(!pending.Succeeded||proof==null)return Unauthorized();
        if(width is <=0 or >1024||height!=width)return BadRequest();
        using var response=await api.SendAsync(HttpMethod.Post,"Account/web/profile-image/prepare",new {registrationToken=proof,width,height});
        if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return StatusCode(502,new {error="The image API is not deployed. Update apiplatform together with siteplatform."});
        return new ContentResult{StatusCode=(int)response.StatusCode,Content=await response.Content.ReadAsStringAsync(),ContentType="application/json"};
    }
    [HttpGet("complete")]
    public async Task<IActionResult> CompleteProfile()
    {
        var pending = await HttpContext.AuthenticateAsync(OnboardingScheme);
        if (!pending.Succeeded) return SignInError("Please sign in to verify your email first.", null);
        ViewBag.VerifiedEmail = pending.Principal!.FindFirstValue("verified_email");
        return View("CompleteProfile", new CompleteProfileModel { FullName = pending.Principal.FindFirstValue("full_name") ?? "" });
    }

    [EnableRateLimiting("auth")]
    [HttpPost("complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(CompleteProfileModel model)
    {
        var pending = await HttpContext.AuthenticateAsync(OnboardingScheme);
        var proof = pending.Principal?.FindFirstValue("registration_token");
        if (!pending.Succeeded || proof == null) return SignInError("Your verification has expired. Please sign in again.", null);
        ViewBag.VerifiedEmail = pending.Principal!.FindFirstValue("verified_email");
        if (!ModelState.IsValid) return View("CompleteProfile", model);
        try
        {
            using var response = await api.SendAsync(HttpMethod.Post, "Account/web/complete", new {
                registrationToken = proof, username = model.Username, fullName = model.FullName, avatarTicket = model.AvatarTicket
            });
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HttpContext.SignOutAsync(OnboardingScheme);
                return SignInError("Your verification has expired or was already used. Please sign in again.", null);
            }
            if (!response.IsSuccessStatusCode)
            {
                var code = "";
                if (response.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.BadRequest)
                {
                    using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    if (error.RootElement.TryGetProperty("code", out var value)) code = value.GetString();
                }
                ModelState.AddModelError("", code switch {
                    "avatar_invalid" => "Your photo upload is incomplete or expired. Select it again, or continue without a photo.",
                    "username_unavailable" => "That username is unavailable. Please choose another.",
                    "account_exists" => "An account now exists for this email. Please sign in again using the link below.",
                    _ => "We couldn’t finish setting up your account. Please try again."
                });
                return View("CompleteProfile", model);
            }
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return await CompleteSignIn(json.RootElement.GetProperty("token").GetString()!, "/");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            ModelState.AddModelError("", "Account setup is temporarily unavailable. Please try again.");
            return View("CompleteProfile", model);
        }
    }

    [EnableRateLimiting("auth")]
    [HttpPost("google")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Google(string credential, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(credential) || credential.Length > 16384) return SignInError("Google sign-in failed. Please try again.", returnUrl);
        try
        {
            using var response = await api.SendAsync(HttpMethod.Post, "Account/google", new { credential });
            if (!response.IsSuccessStatusCode)
                return SignInError(response.StatusCode == System.Net.HttpStatusCode.Conflict
                    ? "Please use email sign-in for this account, or retry if another sign-in was in progress."
                    : "Google sign-in could not be completed. Please retry or use email.", returnUrl);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return await HandleVerifiedIdentity(json.RootElement, returnUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        { return SignInError("Google sign-in is temporarily unavailable. Please use email sign-in.", returnUrl); }
    }

    [HttpGet("email-verification")]
    [HttpGet("verify")]
    [HttpGet("verify/{authToken}")]
    [ResponseCache(Duration = 520)]
    public IActionResult Verify(string? authToken = null, string? token = null)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        // Older emails used ?<jwt> with no parameter name; keep those links usable.
        var suppliedToken = authToken ?? token;
        if (string.IsNullOrEmpty(suppliedToken))
        {
            var rawQuery = Request.QueryString.Value?.TrimStart('?');
            if (rawQuery?.Length <= 4096 && rawQuery != null &&
                System.Text.RegularExpressions.Regex.IsMatch(rawQuery, @"\A[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\z"))
                suppliedToken = rawQuery;
        }
        ViewBag.AuthToken = suppliedToken;
        ViewBag.WebLink = IsWebEmailLink(suppliedToken);
        ViewBag.ReturnUrl = TempData.Peek("LoginReturnUrl") as string ?? "/";
        return View();
    }

    [EnableRateLimiting("auth")]
    [HttpPost("verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyWeb(string authToken, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(authToken) || authToken.Length > 4096) return SignInError("This email link is invalid.", returnUrl);
        try
        {
            using var webResponse = await api.SendAsync(HttpMethod.Post, "Account/web/verify", new { token = authToken });
            if (webResponse.IsSuccessStatusCode)
            {
                using var result = JsonDocument.Parse(await webResponse.Content.ReadAsStringAsync());
                TempData.Remove("LoginReturnUrl");
                return await HandleVerifiedIdentity(result.RootElement, returnUrl, emailVerification: true);
            }
            if (webResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                return SignInError("That email address can no longer be used. Your current email has not changed.", returnUrl);
            if (webResponse.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                return SignInError("Sign-in is temporarily unavailable. Please try again.", returnUrl);
            // Older email links retain their original API route and raw-token response.
            using var response = await api.SendAsync(HttpMethod.Post, "Account/verify/" + Uri.EscapeDataString(authToken));
            if (!response.IsSuccessStatusCode) return SignInError("This link has expired or was already used. Request a new sign-in link.", returnUrl);
            var token = (await response.Content.ReadAsStringAsync()).Trim();
            if (token.StartsWith('"')) token = JsonSerializer.Deserialize<string>(token)!;
            TempData.Remove("LoginReturnUrl");
            return await CompleteSignIn(token, returnUrl, emailVerification: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        { return SignInError("Sign-in is temporarily unavailable. Please try again.", returnUrl); }
    }

    [Authorize]
    [HttpGet("verified")]
    public IActionResult Verified()
    {
        ViewBag.ReturnUrl = SafeReturn(TempData["VerifiedReturnUrl"] as string);
        ViewBag.AndroidIntent = TempData["AndroidIntentUrl"] as string;
        return View();
    }

    [HttpGet("session")]
    public IActionResult Session()
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        return HttpContext.Items.TryGetValue("WebAccount", out var profile) ? Ok(profile) : StatusCode(503);
    }

    [HttpPost("signout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignOutUser()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OnboardingScheme);
        return LocalRedirect("/");
    }

    private async Task<IActionResult> HandleVerifiedIdentity(JsonElement result, string? returnUrl, bool emailVerification = false)
    {
        if (!result.TryGetProperty("requiresProfile", out var required) || !required.GetBoolean())
            return await CompleteSignIn(result.GetProperty("token").GetString()!, returnUrl, emailVerification);
        // This separate encrypted cookie cannot authenticate normal website/API requests.
        var claims = new[] {
            new Claim("registration_token", result.GetProperty("registrationToken").GetString()!),
            new Claim("verified_email", result.GetProperty("email").GetString()!),
            new Claim("full_name", result.GetProperty("fullName").GetString() ?? ""),
            new Claim("return_url", SafeReturn(returnUrl))
        };
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(OnboardingScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, OnboardingScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });
        return LocalRedirect("/Account/complete");
    }

    private async Task<IActionResult> CompleteSignIn(string token, string? returnUrl, bool emailVerification = false)
    {
        using var response = await api.SendAsync(HttpMethod.Get, "Account/me", token: token);
        if (!response.IsSuccessStatusCode) return SignInError("Your account could not be verified. Please try again.", returnUrl);
        var profile = await response.Content.ReadFromJsonAsync<WebAccountModel>();
        if (profile == null) return SignInError("Your account could not be verified.", returnUrl);
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, profile.Username),
            new Claim(ClaimTypes.Name, profile.FullName ?? profile.Username),
            new Claim("api_token", token)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
        await HttpContext.SignOutAsync(OnboardingScheme);
        if (Request.Headers.UserAgent.ToString().Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var handoff = await api.SendAsync(HttpMethod.Post, "Account/web/app-link", token: token);
                if (handoff.IsSuccessStatusCode)
                {
                    using var json = JsonDocument.Parse(await handoff.Content.ReadAsStringAsync());
                    TempData["AndroidIntentUrl"] = AndroidHandoff.BuildIntent(settings.AndroidVerificationUrl,
                        json.RootElement.GetProperty("verificationToken").GetString()!, SafeReturn(returnUrl));
                }
                else logger.LogWarning("Android handoff could not be prepared: {StatusCode}", handoff.StatusCode);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or ArgumentException)
            { logger.LogWarning("Android handoff unavailable; browser sign-in completed."); }
        }
        if (emailVerification)
        {
            TempData["VerifiedReturnUrl"] = SafeReturn(returnUrl);
            return LocalRedirect("/Account/verified");
        }
        return LocalRedirect(SafeReturn(returnUrl));
    }
    // Presentation hint only: the API still verifies every signature and consumes the proof.
    private static bool IsWebEmailLink(string? token)
    {
        if (token == null || token.Length > 4096) return false;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return false;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
            using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
            return json.RootElement.TryGetProperty("JWT_ROLE", out var role) && role.GetString() == "WEB_EMAIL";
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException) { return false; }
    }

    private string SafeReturn(string? value) => Url.IsLocalUrl(value) ? value! : "/";
    private IActionResult SignInError(string message, string? returnUrl, string? method = null)
    {
        ViewBag.Error = message;
        return SignIn(returnUrl, method);
    }
}
