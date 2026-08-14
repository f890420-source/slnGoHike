using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Announcement
{
    public int AnnouncementId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public byte Status { get; set; }
}
