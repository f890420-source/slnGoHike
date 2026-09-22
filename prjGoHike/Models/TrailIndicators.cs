using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class TrailIndicators
{
    public long TrailId { get; set; }

    public long IndicatorId { get; set; }

    public decimal? OverlapRatio { get; set; }

    public decimal? DistanceMeters { get; set; }

    public decimal IndicatorWeightSnapshot { get; set; }

    public decimal? RawScore { get; set; }

    public decimal EvaluatedScore { get; set; }

    public DateTime EvaluatedAt { get; set; }

    public virtual Indicators Indicator { get; set; } = null!;

    public virtual Trails Trail { get; set; } = null!;
}
