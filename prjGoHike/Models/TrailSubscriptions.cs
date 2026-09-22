using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class TrailSubscriptions
{
    public long SubscriptionId { get; set; }

    public long UserId { get; set; }

    public long TrailId { get; set; }

    public bool NotifyLegalChange { get; set; }

    public bool NotifyDisasterAlert { get; set; }

    public bool NotifyNewReport { get; set; }

    public bool IsActive { get; set; }

    public virtual Trails Trail { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
