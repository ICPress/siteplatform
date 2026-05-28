using System.Text.Json.Serialization;

public class ServerSettings
{
    public string MysqlConnectionGorse { get; set; } = "";
    public string MysqlConnectionStoryPop { get; set; } = "";
    public string JWTSecret { get; set; } = "";
    public string CDNAccessKey { get; set; } = "";
    public string IgniteEndpoint { get; set; } = "";
    public string GorseAPIEndpoint { get; set; } = "";
    public string SpacyEndpoint { get; set; } = "";
    public string SwiftAuthEndpoint { get; set; } = "";
    public string SwiftTempAuthUser { get; set; } = "";
    public string SwiftTempAuthKey { get; set; } = "";
    public object? SwiftKeyStoneAuth { get; set; } = null;
    public string SwiftBucketSmallPath { get; set; } = "";
    public string SwiftBucketLargePath { get; set; } = "";
    public string SwiftBucketUserMessagePath { get; set; } = "";

    // CDN publish paths (used when uploading/writing to CDN)
    public string CDNPublishSmallPath { get; set; } = "";
    public string CDNPublishLargePath { get; set; } = "";
    public string CDNPublishUserMessagePath { get; set; } = "";

    // CDN request paths (used when rendering images to clients)
    public string CDNRequestSmallPath { get; set; } = "";
    public string CDNRequestLargePath { get; set; } = "";
    public string CDNRequestUserMessagePath { get; set; } = "";

    public string FirebaseSDKCredentialsJson { get; set; } = "";
    public string SMTPServer { get; set; } = "";
    public int SMTPPort { get; set; } = 465;
    public string SMTPUsername { get; set; } = "";
    public string SMTPPassword { get; set; } = "";
    public string AdminUsername { get; set; } = "";
    public string ImageStaticPath { get; set; } = "";
    public bool RequireArticleSources { get; set; } = true;
    public bool RequireArticleReview { get; set; } = true;
    public string APIEndpoint { get; set; } = "";
    public string SiteEndpoint { get; set; } = "";
    
    public string PublicApiUrl { get; set; } = "";
}
