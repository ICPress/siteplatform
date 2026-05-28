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
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
