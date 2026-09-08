namespace prjGoHike.APIControllers.Forum
{
    public class CreateArticleDto
    {
        public int CategoryId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
