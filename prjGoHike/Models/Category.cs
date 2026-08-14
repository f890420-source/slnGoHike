using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
}
