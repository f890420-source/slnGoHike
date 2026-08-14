using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Article
{
    public int ArticleId { get; set; }

    public long UserId { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public byte Status { get; set; }

    public virtual ICollection<ArticleImage> ArticleImages { get; set; } = new List<ArticleImage>();

    public virtual ICollection<ArticleLike> ArticleLikes { get; set; } = new List<ArticleLike>();

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual User User { get; set; } = null!;
}
