using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class IndicatorSegment
{
    public long IndicatorSegmentId { get; set; }

    public long IndicatorId { get; set; }

    public string? SegmentName { get; set; }

    public Geometry Shape { get; set; } = null!;

    public string? SourceFeatureId { get; set; }

    public byte? SegmentLevel { get; set; }

    public string? Description { get; set; }

    public virtual Indicator Indicator { get; set; } = null!;
}
