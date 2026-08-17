using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class EventLeaderRating
{
    public long ReviewId { get; set; }

    public long EventId { get; set; }

    public long UserId { get; set; }

    public int LeaderRating { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual EventData Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
