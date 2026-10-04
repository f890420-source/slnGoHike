using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class DisasterAlerts
{
    public long AlertId { get; set; }

    public string AlertType { get; set; } = null!;

    public string AlertTitle { get; set; } = null!;

    public string? AlertDescription { get; set; }

    public byte SeverityLevel { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? SourceAgency { get; set; }

    public string? SourceUrl { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<AlertSegments> AlertSegments { get; set; } = new List<AlertSegments>();

    public virtual ICollection<AlertsTrails> AlertsTrails { get; set; } = new List<AlertsTrails>();
}
