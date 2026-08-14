using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class TrailFeature
{
    public long FeatureId { get; set; }

    public long TrailId { get; set; }

    public string FeatureType { get; set; } = null!;

    public string FeatureName { get; set; } = null!;

    public Geometry Location { get; set; } = null!;

    public string? FeatureDescription { get; set; }

    public byte ReliabilityLevel { get; set; }

    public bool IsAvailable { get; set; }

    public string? DataSource { get; set; }

    public virtual Trail Trail { get; set; } = null!;
}
