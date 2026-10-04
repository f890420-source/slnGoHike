namespace prjGoHike.Dtos.Forum
{
    public class UpdateArticleDto
    {
        public int CategoryId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();

        public List<string> KeepImagePaths { get; set; } = new();

        public List<IFormFile> ImageFiles { get; set; } = new();
    }
}
