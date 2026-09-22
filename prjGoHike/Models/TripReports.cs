using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class TripReports
{
    public long ReportId { get; set; }

    public long? TripId { get; set; }

    public long TrailId { get; set; }

    public long? ReporterUserId { get; set; }

    public string SourceType { get; set; } = null!;

    public string ReportType { get; set; } = null!;

    public string ReportContent { get; set; } = null!;

    public Geometry? Location { get; set; }

    public DateTime? OccurredAt { get; set; }

    public string ReviewStatus { get; set; } = null!;

    public long? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int RewardPoints { get; set; }

    public virtual Users? ReporterUser { get; set; }

    public virtual ICollection<ReviewApplications> ReviewApplications { get; set; } = new List<ReviewApplications>();

    public virtual Users? ReviewedByUser { get; set; }

    public virtual Trails Trail { get; set; } = null!;
}
