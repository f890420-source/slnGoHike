using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class ReviewApplication
{
    public long ApplicationId { get; set; }

    public long ApplicantUserId { get; set; }

    public string ApplicationType { get; set; } = null!;

    public long? RelatedHikeDrecordId { get; set; }

    public long? RelatedReportId { get; set; }

    public string Purpose { get; set; } = null!;

    public string? RequestPayloadJson { get; set; }

    public int? RequestedPoints { get; set; }

    public string Status { get; set; } = null!;

    public long? ReviewerUserId { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual User ApplicantUser { get; set; } = null!;

    public virtual HikeRecordDetail? RelatedHikeDrecord { get; set; }

    public virtual TripReport? RelatedReport { get; set; }

    public virtual User? ReviewerUser { get; set; }
}
