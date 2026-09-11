namespace prjGoHike.Models.Dtos
{
    public class UpdateArticleDto
    {
        public int CategoryId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
