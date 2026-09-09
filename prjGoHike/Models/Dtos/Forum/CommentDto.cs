namespace prjGoHike.Dtos
{
    public class CommentDto
    {
        public int CommentId { get; set; }

        public int ArticleId { get; set; }

        public long UserId { get; set; }

        // 留言者資料
        public string UserNickname { get; set; } = string.Empty;

        public string? UserAvatarUrl { get; set; }

        public string Content { get; set; } = string.Empty;

        public int? ParentCommentId { get; set; }

        public long? ReplyToUserId { get; set; }

        public string? ReplyToUserNickname { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdateDate { get; set; }

        public byte Status { get; set; }
    }
}
