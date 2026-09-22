using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class AlertsTrails
{
    public long AlertTrailId { get; set; }

    public long AlertId { get; set; }

    public long TrailId { get; set; }

    public string? ReasonDescription { get; set; }

    public virtual DisasterAlerts Alert { get; set; } = null!;

    public virtual Trails Trail { get; set; } = null!;
}
