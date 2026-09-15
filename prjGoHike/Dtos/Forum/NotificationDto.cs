namespace prjGoHike.Dtos.Forum
{
    public class NotificationDto
    {
        public long NotificationId { get; set; }

        // 發送通知的人
        public long SenderUserId { get; set; }

        public string SenderNickname { get; set; } = string.Empty;

        public string? SenderAvatarUrl { get; set; }

        // 通知對應的文章
        public int? ArticleId { get; set; }

        // 留言 / 回覆通知時使用
        public int? CommentId { get; set; }

        // 1 = 文章收到留言
        // 2 = 留言收到回覆
        // 3 = 文章收到按讚
        public byte Type { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
