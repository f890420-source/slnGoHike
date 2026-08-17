using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class User
{
    public long UserId { get; set; }

    public string Role { get; set; } = null!;

    public string Nickname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string AccountStatus { get; set; } = null!;

    public string AvatarUrl { get; set; } = null!;

    public string AvatarBlurState { get; set; } = null!;

    public string Bio { get; set; } = null!;

    public long CurrentLevelId { get; set; }

    public int TotalXp { get; set; }

    public string RegionPreference { get; set; } = null!;

    public string DifficultyPreference { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime LastActiveAt { get; set; }

    public virtual ICollection<ArticleLike> ArticleLikes { get; set; } = new List<ArticleLike>();

    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();

    public virtual ICollection<Comment> CommentReplyToUsers { get; set; } = new List<Comment>();

    public virtual ICollection<Comment> CommentUsers { get; set; } = new List<Comment>();

    public virtual Level CurrentLevel { get; set; } = null!;

    public virtual ICollection<EventData> EventData { get; set; } = new List<EventData>();

    public virtual ICollection<EventLeaderRating> EventLeaderRatings { get; set; } = new List<EventLeaderRating>();

    public virtual ICollection<EventRegistrationAndMemberList> EventRegistrationAndMemberLists { get; set; } = new List<EventRegistrationAndMemberList>();

    public virtual ICollection<EventReportComplaint> EventReportComplaints { get; set; } = new List<EventReportComplaint>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<GroupAttendance> GroupAttendances { get; set; } = new List<GroupAttendance>();

    public virtual ICollection<HikeRecord> HikeRecords { get; set; } = new List<HikeRecord>();

    public virtual ICollection<Notify> Notifies { get; set; } = new List<Notify>();

    public virtual ICollection<PersonalEquipmentList> PersonalEquipmentLists { get; set; } = new List<PersonalEquipmentList>();

    public virtual ICollection<Report> ReportAdmins { get; set; } = new List<Report>();

    public virtual ICollection<Report> ReportUsers { get; set; } = new List<Report>();

    public virtual ICollection<ReviewApplication> ReviewApplicationApplicantUsers { get; set; } = new List<ReviewApplication>();

    public virtual ICollection<ReviewApplication> ReviewApplicationReviewerUsers { get; set; } = new List<ReviewApplication>();

    public virtual ICollection<SuspensionSchedule> SuspensionSchedules { get; set; } = new List<SuspensionSchedule>();

    public virtual ICollection<TrailSubscription> TrailSubscriptions { get; set; } = new List<TrailSubscription>();

    public virtual ICollection<TripReport> TripReportReporterUsers { get; set; } = new List<TripReport>();

    public virtual ICollection<TripReport> TripReportReviewedByUsers { get; set; } = new List<TripReport>();

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    public virtual ICollection<UserSkillTag> UserSkillTags { get; set; } = new List<UserSkillTag>();
}
