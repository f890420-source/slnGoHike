using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class EventData
{
    public long EventId { get; set; }

    public long MountainId { get; set; }

    public string EventName { get; set; } = null!;

    public int MaximumNumber { get; set; }

    public string ActivityStatus { get; set; } = null!;

    public string ActivityPhoto { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime EventDate { get; set; }

    public bool ReviewRequired { get; set; }

    public string ReviewStatus { get; set; } = null!;

    public bool HasActiveReport { get; set; }

    public long LeaderUserId { get; set; }

    public virtual ICollection<EventLeaderRating> EventLeaderRatings { get; set; } = new List<EventLeaderRating>();

    public virtual ICollection<EventRegistrationAndMemberList> EventRegistrationAndMemberLists { get; set; } = new List<EventRegistrationAndMemberList>();

    public virtual ICollection<EventReportComplaint> EventReportComplaints { get; set; } = new List<EventReportComplaint>();

    public virtual ICollection<GroupAttendance> GroupAttendances { get; set; } = new List<GroupAttendance>();

    public virtual User LeaderUser { get; set; } = null!;

    public virtual Mountain Mountain { get; set; } = null!;

    public virtual ICollection<SuspensionSchedule> SuspensionSchedules { get; set; } = new List<SuspensionSchedule>();
}
