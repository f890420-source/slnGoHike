using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class GroupAttendance
{
    public long GroupId { get; set; }

    public long UserId { get; set; }

    public string AttendanceStatus { get; set; } = null!;

    public virtual EventData Group { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
