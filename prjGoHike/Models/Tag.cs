using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Tag
{
    public int TagId { get; set; }

    public string TagName { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
}
