using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class AlertsTrail
{
    public long AlertTrailId { get; set; }

    public long AlertId { get; set; }

    public long TrailId { get; set; }

    public string? ReasonDescription { get; set; }

    public virtual DisasterAlert Alert { get; set; } = null!;

    public virtual Trail Trail { get; set; } = null!;
}
