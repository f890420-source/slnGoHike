namespace prjGoHike.Models.Dtos.Forum
{
    public class CreateCommentDto
    {
        public int ArticleId { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}
