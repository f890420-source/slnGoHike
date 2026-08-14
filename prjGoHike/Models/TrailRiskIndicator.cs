using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class TrailRiskIndicator
{
    public long TrailId { get; set; }

    public long RiskIndicatorId { get; set; }

    public decimal OverlapRatio { get; set; }

    public decimal? DistanceFromTrailMeters { get; set; }

    public decimal IndicatorWeightSnapshot { get; set; }

    public decimal RiskScore { get; set; }

    public DateTime EvaluatedAt { get; set; }

    public virtual RiskIndicator RiskIndicator { get; set; } = null!;

    public virtual Trail Trail { get; set; } = null!;
}
