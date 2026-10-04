using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Trails
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

    public bool IsPublished { get; set; }

    public virtual ICollection<AlertsTrails> AlertsTrails { get; set; } = new List<AlertsTrails>();

    public virtual ICollection<HikeRecordDetails> HikeRecordDetails { get; set; } = new List<HikeRecordDetails>();

    public virtual ICollection<TrailFeatures> TrailFeatures { get; set; } = new List<TrailFeatures>();

    public virtual ICollection<TrailIndicators> TrailIndicators { get; set; } = new List<TrailIndicators>();

    public virtual ICollection<TrailSegments> TrailSegments { get; set; } = new List<TrailSegments>();

    public virtual ICollection<TrailSubscriptions> TrailSubscriptions { get; set; } = new List<TrailSubscriptions>();

    public virtual ICollection<TripReports> TripReports { get; set; } = new List<TripReports>();
}
