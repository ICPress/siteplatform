using System.Net.Http.Headers;
namespace siteplatform.Services;
public sealed class PlatformApi(HttpClient client, ServerSettings settings)
{
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null, string? token = null)
    {
        using var request = new HttpRequestMessage(method, settings.PublicApiUrl.TrimEnd('/') + "/" + path.TrimStart('/'));
        if (body != null) request.Content = JsonContent.Create(body);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }
    public async Task<HttpResponseMessage> SendContentAsync(HttpMethod method, string path, HttpContent content, string token)
    {
        using var request = new HttpRequestMessage(method, settings.PublicApiUrl.TrimEnd('/') + "/" + path.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;
        return await client.SendAsync(request);
    }
}
