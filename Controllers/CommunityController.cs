using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using siteplatform.Models;
using siteplatform.Services;
using System.Security.Claims;
using System.Text.Json;
namespace siteplatform.Controllers;
[Authorize]
public class CommunityController(PlatformApi api):Controller
{
    private string Username=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string Token=>User.FindFirstValue("api_token")!;
    public override async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next)
    {
        var executed=await next();
        if(executed.Exception is HttpRequestException or TaskCanceledException or JsonException){executed.ExceptionHandled=true;Response.StatusCode=503;executed.Result=View("Unavailable");}
    }
    private async Task<T?> Get<T>(string path,bool signedIn=true)
    {
        using var response=await api.SendAsync(HttpMethod.Get,path,token:signedIn?Token:null);
        if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return default;
        response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<T>();
    }
    [HttpGet("/profile")]
    public IActionResult Mine()=>RedirectToAction(nameof(Profile),new {username=Username});
    [AllowAnonymous,HttpGet("/u/{username}"),ResponseCache(Duration=120,VaryByHeader="Cookie")]
    public async Task<IActionResult> Profile(string username)
    {
        if(User.Identity?.IsAuthenticated==true)Response.Headers.CacheControl="private, no-cache";
        var profile=await Get<PublicProfileModel>("Community/profile/"+Uri.EscapeDataString(username),false);return profile==null?NotFound():View(profile);
    }
    [HttpGet("/profile/edit")]
    public async Task<IActionResult> Edit()
    {
        var profile=await Get<PublicProfileModel>("Community/profile/"+Uri.EscapeDataString(Username));if(profile==null)return NotFound();
        return View(new EditProfileModel{Profile=profile,FullName=profile.FullName,Description=profile.Description,Email=(HttpContext.Items["WebAccount"] as WebAccountModel)?.Email??""});
    }
    [HttpPost("/profile/edit"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileModel model)
    {
        if(ModelState.IsValid)
        {
            using var response=await api.SendAsync(HttpMethod.Post,"Community/profile",new {model.FullName,model.Description},Token);
            if(response.IsSuccessStatusCode){TempData["ProfileMessage"]="Your profile has been updated.";return RedirectToAction(nameof(Profile),new {username=Username});}
            ModelState.AddModelError("","Your profile could not be saved. Please try again.");
        }
        model.Profile=await Get<PublicProfileModel>("Community/profile/"+Uri.EscapeDataString(Username))??new();model.Email=(HttpContext.Items["WebAccount"] as WebAccountModel)?.Email??"";return View(model);
    }
    [HttpPost("/profile/image/{kind}/prepare",Name="ProfileImagePrepare"),ValidateAntiForgeryToken,EnableRateLimiting("auth")]
    public async Task<IActionResult> PrepareImage(string kind,int width,int height)
    {
        Response.Headers.CacheControl="no-store";
        if(kind is not ("avatar" or "cover")||width is <=0 or >1024||height is <=0 or >1024)return BadRequest();
        using var response=await api.SendAsync(HttpMethod.Post,"Community/profile/image/"+kind+"/prepare",new {width,height},Token);
        // An upstream 404 is a mismatched API deployment, not a missing website route.
        if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return StatusCode(502,new {error="The image API is not deployed. Update apiplatform together with siteplatform."});
        return new ContentResult{StatusCode=(int)response.StatusCode,Content=await response.Content.ReadAsStringAsync(),ContentType="application/json"};
    }
    [HttpPost("/profile/image/{kind}/complete",Name="ProfileImageComplete"),ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteImage(string kind,string ticket)
    {
        if(kind is not ("avatar" or "cover")||string.IsNullOrWhiteSpace(ticket)||ticket.Length>2048)return BadRequest();
        using var response=await api.SendAsync(HttpMethod.Post,"Community/profile/image/"+kind+"/complete",new {ticket},Token);
        if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return StatusCode(502,new {error="The image API is not deployed. Update apiplatform together with siteplatform."});
        return new ContentResult{StatusCode=(int)response.StatusCode,Content=await response.Content.ReadAsStringAsync(),ContentType="application/json"};
    }
    [HttpPost("/profile/image/{kind}/remove"),ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveImage(string kind)
    {
        if(kind is not ("avatar" or "cover"))return BadRequest();using var response=await api.SendAsync(HttpMethod.Delete,"Community/profile/image/"+kind,token:Token);
        TempData["ProfileMessage"]=response.IsSuccessStatusCode?"Image removed.":"The image could not be removed. Please retry.";return RedirectToAction(nameof(Edit));
    }
    [HttpPost("/profile/email"),ValidateAntiForgeryToken,EnableRateLimiting("auth")]
    public async Task<IActionResult> Email(string? email)
    {
        if(email==null||email.Length>255||!System.Net.Mail.MailAddress.TryCreate(email,out _))TempData["ProfileMessage"]="Enter a valid email address.";
        else
        {
            using var response=await api.SendAsync(HttpMethod.Post,"Community/email",new {email},Token);
            TempData["ProfileMessage"]=response.IsSuccessStatusCode?"Check your new email address for a verification link. Your current email remains active until you verify the new one.":response.StatusCode==System.Net.HttpStatusCode.Conflict?"That email address cannot be used.":"The email change could not be requested. Check the address and try again shortly.";
        }
        return RedirectToAction(nameof(Edit));
    }
    [HttpGet("/profile/delete")]
    public IActionResult Delete()=>View();
    [HttpPost("/profile/delete"),ValidateAntiForgeryToken,EnableRateLimiting("auth")]
    public async Task<IActionResult> Delete(string? confirmation)
    {
        if(!string.Equals(confirmation,Username,StringComparison.Ordinal)){ModelState.AddModelError("","Enter your username exactly to confirm deletion.");return View();}
        using var response=await api.SendAsync(HttpMethod.Delete,"Account",token:Token);
        if(!response.IsSuccessStatusCode){ModelState.AddModelError("","Your account could not be deleted. Please try again.");return View();}
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);await HttpContext.SignOutAsync(AccountController.OnboardingScheme);
        TempData["AccountDeleted"]=true;return Redirect("/Account/deleted");
    }
    [AllowAnonymous,HttpGet("/Account/deleted")]
    public IActionResult Deleted()=>TempData["AccountDeleted"] is true?View():Redirect("/");
    [HttpGet("/notifications")]
    public async Task<IActionResult> Notifications(uint? before=null)=>View(await Get<List<NoticeModel>>("Community/notifications?before="+before)??[]);
    [HttpPost("/notifications/read"),ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadNotifications(uint[] ids)
    {
        using var response=await api.SendAsync(HttpMethod.Post,"Community/notifications/read",new {ids},Token);return StatusCode((int)response.StatusCode);
    }
    [HttpGet("/messages")]
    public async Task<IActionResult> Messages(uint? before=null)=>View(await Get<List<InboxModel>>("Community/messages?before="+before)??[]);
    [HttpGet("/messages/{username}")]
    public async Task<IActionResult> Conversation(string username,uint? before=null)
    {
        var peer=await Get<PublicProfileModel>("Community/profile/"+Uri.EscapeDataString(username))??new PublicProfileModel{Username=username};
        return View(new ConversationModel{Peer=peer,Items=await Get<List<ConversationItem>>("Community/messages/"+Uri.EscapeDataString(username)+"?before="+before)??[]});
    }
    [HttpPost("/messages/{username}/read"),ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadMessages(string username,uint[] ids)
    {
        using var response=await api.SendAsync(HttpMethod.Post,"Community/messages/"+Uri.EscapeDataString(username)+"/read",new {ids},Token);return StatusCode((int)response.StatusCode);
    }
}
