using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class SuspensionSchedule
{
    public long BanId { get; set; }

    public long UserId { get; set; }

    public long? EventId { get; set; }

    public string Reason { get; set; } = null!;

    public string SuspensionStatus { get; set; } = null!;

    public DateTime SuspensionExpirationTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual EventData? Event { get; set; }

    public virtual User User { get; set; } = null!;
}
