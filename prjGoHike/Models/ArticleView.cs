using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class ArticleView
{
    public long ViewId { get; set; }

    public int ArticleId { get; set; }

    public long? UserId { get; set; }

    public DateTime ViewedDate { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual User? User { get; set; }
}
