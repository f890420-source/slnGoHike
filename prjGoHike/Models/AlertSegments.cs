using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class AlertSegments
{
    public long AlertSegmentId { get; set; }

    public long AlertId { get; set; }

    public string? SegmentName { get; set; }

    public Geometry Shape { get; set; } = null!;

    public string? SourceFeatureId { get; set; }

    public byte? SegmentLevel { get; set; }

    public string? Description { get; set; }

    public virtual DisasterAlerts Alert { get; set; } = null!;
}
