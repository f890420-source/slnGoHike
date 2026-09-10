namespace prjGoHike.Models.Dtos.Forum
{
    public class HotArticleDto
    {
        public int ArticleId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public int LikeCount { get; set; }

        public int FavoriteCount { get; set; }

        public int CommentCount { get; set; }
    }
}
