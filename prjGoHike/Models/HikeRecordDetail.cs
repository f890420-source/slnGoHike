using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace prjGoHike.Models;

public partial class HikeRecordDetail
{
    public long HikerecDetailId { get; set; }

    public long HikeRecordId { get; set; }

    public long TrailId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Status { get; set; } = null!;

    public Geometry? UploadedTrack { get; set; }

    public decimal? CalculatedRiskScore { get; set; }

    public virtual HikeRecord HikeRecord { get; set; } = null!;

    public virtual ICollection<ReviewApplication> ReviewApplications { get; set; } = new List<ReviewApplication>();

    public virtual Trail Trail { get; set; } = null!;
}
