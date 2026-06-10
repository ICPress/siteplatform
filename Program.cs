using System.Net;
using SimpleMvcSitemap;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ISitemapProvider, SitemapProvider>();

// Bind ServerSettings from appsettings.json
builder.Services.Configure<ServerSettings>(
    builder.Configuration.GetSection(nameof(ServerSettings)));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServerSettings>>().Value);

builder.Services.AddResponseCaching();

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

app.UseAuthorization();

app.UseResponseCaching();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
