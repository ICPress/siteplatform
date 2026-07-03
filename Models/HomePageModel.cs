public class CategorySection
{
    public string Category { get; set; } = "";
    public List<StoryPublishedModel> Stories { get; set; } = new();
}

public class HomePageModel
{
    public List<StoryPublishedModel> LatestStories { get; set; } = new();
    public List<TrendingTagModel> TrendingTags { get; set; } = new();
    public List<TrendingTagModel> TrendingCategories { get; set; } = new();
    public List<CategorySection> CategorySections { get; set; } = new();
}
