using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class BackgroundJobRun
{
    public long Id { get; set; }

    public string JobType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? HangfireJobId { get; set; }

    public decimal DistanceMeters { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string? ErrorMessage { get; set; }
}
