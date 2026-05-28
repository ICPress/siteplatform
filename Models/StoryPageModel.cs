public class StoryPageModel
{
    public StoryPublishedModel? Article { get; set; }
    public List<StoryPublishedModel> SimilarArticles { get; set; } = new();
}