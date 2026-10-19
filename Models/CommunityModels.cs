using System.ComponentModel.DataAnnotations;
namespace siteplatform.Models;
public class PublicProfileModel
{
    public string Username {get;set;}="";
    public string? FullName {get;set;}
    public string? Description {get;set;}
    public string? AvatarUrl {get;set;}
    public string? CoverUrl {get;set;}
    public DateTimeOffset MemberSince {get;set;}
    public long Followers {get;set;}
    public long Following {get;set;}
    public long ArticlesPublished {get;set;}
    public string DisplayName=>string.IsNullOrWhiteSpace(FullName)?Username:FullName;
}
public class EditProfileModel
{
    [StringLength(200)]public string? FullName {get;set;}
    [StringLength(500)]public string? Description {get;set;}
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever,Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public PublicProfileModel Profile {get;set;}=new();
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever,Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string Email {get;set;}="";
}
public class NoticeModel
{
    public uint Id {get;set;}
    public string Title {get;set;}="";
    public string? Detail {get;set;}
    public string? Href {get;set;}
    public string? AvatarUrl {get;set;}
    public bool Read {get;set;}
    public DateTimeOffset Timestamp {get;set;}
}
public class InboxModel
{
    public uint Id {get;set;}
    public string Username {get;set;}="";
    public string? FullName {get;set;}
    public string? AvatarUrl {get;set;}
    public string Preview {get;set;}="";
    public DateTimeOffset Timestamp {get;set;}
    public long Unread {get;set;}
    public string DisplayName=>string.IsNullOrWhiteSpace(FullName)?Username:FullName;
}
public class ConversationItem
{
    public uint Id {get;set;}
    public string Author {get;set;}="";
    public string Content {get;set;}="";
    public string[] Images {get;set;}=[];
    public bool Deleted {get;set;}
    public bool Read {get;set;}
    public DateTimeOffset Timestamp {get;set;}
}
public class ConversationModel
{
    public PublicProfileModel Peer {get;set;}=new();
    public List<ConversationItem> Items {get;set;}=[];
}
