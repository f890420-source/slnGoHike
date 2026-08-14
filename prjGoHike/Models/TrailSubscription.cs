using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class TrailSubscription
{
    public long SubscriptionId { get; set; }

    public long UserId { get; set; }

    public long TrailId { get; set; }

    public bool NotifyLegalChange { get; set; }

    public bool NotifyDisasterAlert { get; set; }

    public bool NotifyNewReport { get; set; }

    public bool IsActive { get; set; }

    public virtual Trail Trail { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
