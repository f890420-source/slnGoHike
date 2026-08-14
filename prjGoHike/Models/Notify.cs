using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Notify
{
    public long NotificationId { get; set; }

    public long UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string RelatedFormType { get; set; } = null!;

    public long RelatedId { get; set; }

    public string UsingPipeline { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
