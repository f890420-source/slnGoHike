using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Users
{
    public long UserId { get; set; }

    public string Role { get; set; } = null!;

    public string Nickname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? AccountStatus { get; set; }

    public string? AvatarUrl { get; set; }

    public string? AvatarBlurState { get; set; }

    public string? Bio { get; set; }

    public long CurrentLevelId { get; set; }

    public int TotalXp { get; set; }

    public string? RegionPreference { get; set; }

    public string? DifficultyPreference { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastActiveAt { get; set; }

    public long? DisplayedAchievementId { get; set; }

    public virtual ICollection<Article> Article { get; set; } = new List<Article>();

    public virtual ICollection<ArticleLike> ArticleLike { get; set; } = new List<ArticleLike>();

    public virtual ICollection<ArticleView> ArticleView { get; set; } = new List<ArticleView>();

    public virtual ICollection<Comment> CommentReplyToUser { get; set; } = new List<Comment>();

    public virtual ICollection<Comment> CommentUser { get; set; } = new List<Comment>();

    public virtual Levels CurrentLevel { get; set; } = null!;

    public virtual Achievements? DisplayedAchievement { get; set; }

    public virtual ICollection<EventLeaderRating> EventLeaderRating { get; set; } = new List<EventLeaderRating>();

    public virtual ICollection<EventRegistrationAndMemberList> EventRegistrationAndMemberList { get; set; } = new List<EventRegistrationAndMemberList>();

    public virtual ICollection<EventReportComplaint> EventReportComplaint { get; set; } = new List<EventReportComplaint>();

    public virtual ICollection<Favorite> Favorite { get; set; } = new List<Favorite>();

    public virtual ICollection<GroupAttendance> GroupAttendance { get; set; } = new List<GroupAttendance>();

    public virtual ICollection<HikeRecords> HikeRecords { get; set; } = new List<HikeRecords>();

    public virtual ICollection<Notification> NotificationSenderUser { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUser { get; set; } = new List<Notification>();

    public virtual ICollection<Notify> Notify { get; set; } = new List<Notify>();

    public virtual ICollection<PersonalEquipmentLists> PersonalEquipmentLists { get; set; } = new List<PersonalEquipmentLists>();

    public virtual ICollection<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();

    public virtual ICollection<Report> ReportAdmin { get; set; } = new List<Report>();

    public virtual ICollection<Report> ReportUser { get; set; } = new List<Report>();

    public virtual ICollection<ReviewApplications> ReviewApplicationsApplicantUser { get; set; } = new List<ReviewApplications>();

    public virtual ICollection<ReviewApplications> ReviewApplicationsReviewerUser { get; set; } = new List<ReviewApplications>();

    public virtual ICollection<SuspensionSchedule> SuspensionSchedule { get; set; } = new List<SuspensionSchedule>();

    public virtual ICollection<TrailSubscriptions> TrailSubscriptions { get; set; } = new List<TrailSubscriptions>();

    public virtual ICollection<TripReports> TripReportsReporterUser { get; set; } = new List<TripReports>();

    public virtual ICollection<TripReports> TripReportsReviewedByUser { get; set; } = new List<TripReports>();

    public virtual ICollection<UserAchievements> UserAchievements { get; set; } = new List<UserAchievements>();

    public virtual ICollection<UserSkillTags> UserSkillTags { get; set; } = new List<UserSkillTags>();
}
