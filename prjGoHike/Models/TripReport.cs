using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class TripReport
{
    public long ReportId { get; set; }

    public long? TripId { get; set; }

    public long TrId { get; set; }

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

    public virtual User? ReporterUser { get; set; }

    public virtual ICollection<ReviewApplication> ReviewApplications { get; set; } = new List<ReviewApplication>();

    public virtual User? ReviewedByUser { get; set; }

    public virtual Trail Tr { get; set; } = null!;
}
