using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using siteplatform.Services;
using siteplatform.Models;
using System.Net;
using SimpleMvcSitemap;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddSingleton<ISitemapProvider, SitemapProvider>();

// Bind ServerSettings from appsettings.json
builder.Services.Configure<ServerSettings>(
    builder.Configuration.GetSection(nameof(ServerSettings)));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServerSettings>>().Value);

builder.Services.AddHttpClient<PlatformApi>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "__Host-ICPress.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/signin";
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest") context.Response.StatusCode = 401;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnValidatePrincipal = async context =>
    {
        var token = context.Principal?.FindFirst("api_token")?.Value;
        if (token == null) { context.RejectPrincipal(); return; }
        try
        {
            var api = context.HttpContext.RequestServices.GetRequiredService<PlatformApi>();
            using var response = await api.SendAsync(HttpMethod.Get, "Account/me", token: token);
            if (!response.IsSuccessStatusCode)
            {
                context.RejectPrincipal();
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    await context.HttpContext.SignOutAsync();
                return;
            }
            var account = await response.Content.ReadFromJsonAsync<WebAccountModel>();
            if (account == null) { context.RejectPrincipal(); return; }
            context.HttpContext.Items["WebAccount"] = account;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        { context.RejectPrincipal(); }
    };
}).AddCookie(siteplatform.Controllers.AccountController.OnboardingScheme, options =>
{
    options.Cookie.Name = "__Host-ICPress.Onboarding";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/signin";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
});

var serverSettings =
    builder.Configuration.GetSection(nameof(ServerSettings))
                     .Get<ServerSettings>(); //parse serverSettings from json

if (serverSettings != null)
{
    if (!string.IsNullOrEmpty(serverSettings.SiteEndpoint))
    {
        // Sets the hosting endpoint
        builder.WebHost.UseUrls(serverSettings.SiteEndpoint);
    }
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 year 
        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});

app.UseRouting();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        // Public pages retain their original cache durations. Personalized pages
        // must not enter shared caches now that the header contains account data.
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var cacheControl = context.Response.Headers.CacheControl.ToString();
            if (!cacheControl.Contains("no-store", StringComparison.OrdinalIgnoreCase))
                context.Response.Headers.CacheControl = string.IsNullOrWhiteSpace(cacheControl)
                    ? "private, no-cache"
                    : cacheControl.Replace("public", "private", StringComparison.OrdinalIgnoreCase);
        }
        if (context.Request.Path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase))
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
        return Task.CompletedTask;
    });
    await next();
});
app.UseAuthentication();
app.UseAuthorization();



// Explicitly map named attribute endpoints, including browser image preparation/commit.
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
