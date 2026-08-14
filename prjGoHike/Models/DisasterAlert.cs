using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class DisasterAlert
{
    public long AlertId { get; set; }

    public string AlertType { get; set; } = null!;

    public string AlertTitle { get; set; } = null!;

    public string? AlertDescription { get; set; }

    public byte SeverityLevel { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public Geometry? AffectedArea { get; set; }

    public string? SourceAgency { get; set; }

    public string? SourceUrl { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<AlertsTrail> AlertsTrails { get; set; } = new List<AlertsTrail>();
}
