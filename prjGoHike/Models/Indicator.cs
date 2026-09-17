using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Indicator
{
    public long IndicatorId { get; set; }

    public string IndicatorName { get; set; } = null!;

    public string IndicatorType { get; set; } = null!;

    public decimal Weight { get; set; }

    public byte? IndicatorLevel { get; set; }

    public string? IndicatorDescription { get; set; }

    public string? DataSource { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<IndicatorSegment> IndicatorSegments { get; set; } = new List<IndicatorSegment>();

    public virtual ICollection<TrailIndicator> TrailIndicators { get; set; } = new List<TrailIndicator>();
}
