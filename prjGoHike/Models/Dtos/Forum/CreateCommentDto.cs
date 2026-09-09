namespace prjGoHike.Dtos
{
    public class CreateCommentDto
    {
        public int ArticleId { get; set; }

        public string Content { get; set; } = string.Empty;

        // 回覆哪一則主留言
        // null 代表這是一則普通留言
        public int? ParentCommentId { get; set; }

        // 被回覆的使用者
        // null 代表這是一則普通留言
        public long? ReplyToUserId { get; set; }
    }
}
