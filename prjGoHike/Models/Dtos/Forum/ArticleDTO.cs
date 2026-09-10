namespace prjGoHike.Models.Dtos.Forum
{
    public class ArticleDto
    {
        public int ArticleId { get; set; }

        public long UserId { get; set; }

        public int CategoryId { get; set; }

        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdateDate { get; set; }

        public byte Status { get; set; }

        public string? CategoryName { get; set; }
        public List<string> ImagePaths { get; set; } = new();
        public int LikeCount { get; set; }

        public int FavoriteCount { get; set; }
        public int CommentCount { get; set; }
        public string UserNickname { get; set; } = string.Empty;

        public string? UserAvatarUrl { get; set; }
    }
}

