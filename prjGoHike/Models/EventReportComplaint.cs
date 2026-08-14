using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class EventReportComplaint
{
    public long ReportEventId { get; set; }

    public long UserId { get; set; }

    public long EventId { get; set; }

    public string ReportReason { get; set; } = null!;

    public string EvidenceUrl { get; set; } = null!;

    public string ReportStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual EventDatum Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
