public class PageModel {
    public PageModel(List<StoryPublishedModel> stories, int currentPage, int[] pageRange, int requestedStoryCount)
    {
        this.Stories = stories;
        this.CurrentPage = currentPage;
        this.PageRange = pageRange;
        this.RequestedStoryCount = requestedStoryCount;
    }
    public List<StoryPublishedModel> Stories { get; set; }
    public int CurrentPage;

    public int[] PageRange;

    public int RequestedStoryCount = 0; 

    public string? Query { get; set; } = null;
    
}