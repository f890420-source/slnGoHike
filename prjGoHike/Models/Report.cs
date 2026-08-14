using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace prjGoHike.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public long UserId { get; set; }

    public int ArticleId { get; set; }

    [DisplayName("原因")]
    public string Reason { get; set; } = null!;

    public string? Reply { get; set; }

    public long? AdminId { get; set; }

    public DateTime? ReviewDate { get; set; }

    [DisplayName("狀態")]
    public byte Status { get; set; }

    [DisplayName("日期")]
    public DateTime CreatedDate { get; set; }

    public virtual User? Admin { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
