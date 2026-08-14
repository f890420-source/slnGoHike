using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class Trail
{
    public long TrailId { get; set; }

    public string TrailName { get; set; } = null!;

    public string Region { get; set; } = null!;

    public int DifficultyLevel { get; set; }

    public decimal? DistanceKm { get; set; }

    public decimal? EstimatedHours { get; set; }

    public bool PermitRequired { get; set; }

    public bool GuideRequired { get; set; }

    public string? RegulationNote { get; set; }

    public Geometry? TrailPath { get; set; }

    public bool IsPublished { get; set; }

    public virtual ICollection<AlertsTrail> AlertsTrails { get; set; } = new List<AlertsTrail>();

    public virtual ICollection<HikeRecordDetail> HikeRecordDetails { get; set; } = new List<HikeRecordDetail>();

    public virtual ICollection<TrailFeature> TrailFeatures { get; set; } = new List<TrailFeature>();

    public virtual ICollection<TrailRiskIndicator> TrailRiskIndicators { get; set; } = new List<TrailRiskIndicator>();

    public virtual ICollection<TrailSubscription> TrailSubscriptions { get; set; } = new List<TrailSubscription>();

    public virtual ICollection<TripReport> TripReports { get; set; } = new List<TripReport>();
}
