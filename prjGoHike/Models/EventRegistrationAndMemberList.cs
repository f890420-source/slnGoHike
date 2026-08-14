using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class EventRegistrationAndMemberList
{
    public long SignUpId { get; set; }

    public long UserId { get; set; }

    public long EventId { get; set; }

    public int RegistrationStatus { get; set; }

    public string EmergencyContact { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual EventDatum Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
