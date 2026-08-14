using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Favorite
{
    public int FavoriteId { get; set; }

    public long UserId { get; set; }

    public int ArticleId { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
