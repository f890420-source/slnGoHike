using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class TrailSegment
{
    public int TrailSegmentId { get; set; }

    public long TrailId { get; set; }

    public Geometry RoutePath { get; set; } = null!;

    public string? Source { get; set; }

    public string? SourceId { get; set; }

    public string? SourceUrl { get; set; }

    public virtual Trail Trail { get; set; } = null!;
}
