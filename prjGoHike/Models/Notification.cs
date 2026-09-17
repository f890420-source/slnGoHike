using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public long UserId { get; set; }

    public long SenderUserId { get; set; }

    public int? ArticleId { get; set; }

    public int? CommentId { get; set; }

    public byte Type { get; set; }

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Article? Article { get; set; }

    public virtual Comment? Comment { get; set; }

    public virtual User SenderUser { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
