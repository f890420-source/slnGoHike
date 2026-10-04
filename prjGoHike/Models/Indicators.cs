using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Indicators
{
    public long IndicatorId { get; set; }

    public string IndicatorName { get; set; } = null!;

    public string IndicatorType { get; set; } = null!;

    public decimal Weight { get; set; }

    public byte? IndicatorLevel { get; set; }

    public string? IndicatorDescription { get; set; }

    public string? DataSource { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<IndicatorSegments> IndicatorSegments { get; set; } = new List<IndicatorSegments>();

    public virtual ICollection<TrailIndicators> TrailIndicators { get; set; } = new List<TrailIndicators>();
}
