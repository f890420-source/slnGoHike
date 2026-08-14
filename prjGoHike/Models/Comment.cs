using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int ArticleId { get; set; }

    public long UserId { get; set; }

    public string Content { get; set; } = null!;

    public int? ParentCommentId { get; set; }

    public long? ReplyToUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public byte Status { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual ICollection<CommentImage> CommentImages { get; set; } = new List<CommentImage>();

    public virtual ICollection<Comment> InverseParentComment { get; set; } = new List<Comment>();

    public virtual Comment? ParentComment { get; set; }

    public virtual User? ReplyToUser { get; set; }

    public virtual User User { get; set; } = null!;
}
