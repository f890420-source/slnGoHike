using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class CommentImage
{
    public int ImageId { get; set; }

    public int CommentId { get; set; }

    public string ImagePath { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public virtual Comment Comment { get; set; } = null!;
}
