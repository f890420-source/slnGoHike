using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class ArticleImage
{
    public int ImageId { get; set; }

    public int ArticleId { get; set; }

    public string ImagePath { get; set; } = null!;

    public int SortOrder { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Article Article { get; set; } = null!;
}
