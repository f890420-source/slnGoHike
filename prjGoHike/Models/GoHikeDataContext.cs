using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using static prjGoHike.Models.UserPermissions;

namespace prjGoHike.Models;

public partial class GoHikeDataContext : DbContext
{
    public DbSet<prjGoHike.Models.CEventDataWarp> CEventDataWarp { get; set; } = default!;
    public GoHikeDataContext()
    {
    }

    public GoHikeDataContext(DbContextOptions<GoHikeDataContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Achievement> Achievements { get; set; }

    public virtual DbSet<AlertSegment> AlertSegments { get; set; }

    public virtual DbSet<AlertsTrail> AlertsTrails { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Article> Articles { get; set; }

    public virtual DbSet<ArticleImage> ArticleImages { get; set; }

    public virtual DbSet<ArticleLike> ArticleLikes { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<CommentImage> CommentImages { get; set; }

    public virtual DbSet<DisasterAlert> DisasterAlerts { get; set; }

    public virtual DbSet<Equipment> Equipments { get; set; }

    public virtual DbSet<EquipmentCategory> EquipmentCategories { get; set; }

    public virtual DbSet<EventData> EventData { get; set; }

    public virtual DbSet<EventLeaderRating> EventLeaderRatings { get; set; }

    public virtual DbSet<EventRegistrationAndMemberList> EventRegistrationAndMemberLists { get; set; }

    public virtual DbSet<EventReportComplaint> EventReportComplaints { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<GroupAttendance> GroupAttendances { get; set; }

    public virtual DbSet<HikeRecord> HikeRecords { get; set; }

    public virtual DbSet<HikeRecordDetail> HikeRecordDetails { get; set; }

    public virtual DbSet<Indicator> Indicators { get; set; }

    public virtual DbSet<IndicatorSegment> IndicatorSegments { get; set; }

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<Mountain> Mountains { get; set; }

    public virtual DbSet<MountainEquipmentSuggestion> MountainEquipmentSuggestions { get; set; }

    public virtual DbSet<Notify> Notifies { get; set; }

    public virtual DbSet<PersonalEquipmentDetail> PersonalEquipmentDetails { get; set; }

    public virtual DbSet<PersonalEquipmentList> PersonalEquipmentLists { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReviewApplication> ReviewApplications { get; set; }

    public virtual DbSet<SkillTag> SkillTags { get; set; }

    public virtual DbSet<SuspensionSchedule> SuspensionSchedules { get; set; }

    public virtual DbSet<Trail> Trails { get; set; }

    public virtual DbSet<TrailFeature> TrailFeatures { get; set; }

    public virtual DbSet<TrailIndicator> TrailIndicators { get; set; }

    public virtual DbSet<TrailSegment> TrailSegments { get; set; }

    public virtual DbSet<TrailSubscription> TrailSubscriptions { get; set; }

    public virtual DbSet<TripReport> TripReports { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAchievement> UserAchievements { get; set; }

    public virtual DbSet<UserSkillTag> UserSkillTags { get; set; }
    public virtual DbSet<ArticleView> ArticleViews { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:GoHikeDataContext", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TPH 繼承設定:用 Role 欄位的值決定要 new 成哪個子類
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("Role")
            .HasValue<Member>("一般會員")
            .HasValue<EventLeader>("團主")
            .HasValue<Admin>("管理員");

        modelBuilder.Entity<Level>().HasData(
        new Level { LevelId = 2, LevelName = "新手登山客", MinXp = 0, MaxXp = 100 },
        new Level { LevelId = 3, LevelName = "初階登山客", MinXp = 101, MaxXp = 500 },
        new Level { LevelId = 4, LevelName = "進階登山客", MinXp = 501, MaxXp = 1500 },
        new Level { LevelId = 5, LevelName = "資深登山客", MinXp = 1501, MaxXp = 3000 },
        new Level { LevelId = 6, LevelName = "登山達人", MinXp = 3001, MaxXp = 99999 });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany() // User.txt不用加collection屬性，單向關聯
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.ToTable("achievements");

            entity.Property(e => e.AchievementId).HasColumnName("achievement_id");
            entity.Property(e => e.ConditionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("condition_type");
            entity.Property(e => e.ConditionValue)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("condition_value");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Rarity)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("rarity");
        });

        modelBuilder.Entity<AlertSegment>(entity =>
        {
            entity.HasIndex(e => e.Shape, "SIX_AlertSegments_Shape");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SegmentName).HasMaxLength(200);
            entity.Property(e => e.SourceFeatureId).HasMaxLength(100);

            entity.HasOne(d => d.Alert).WithMany(p => p.AlertSegments)
                .HasForeignKey(d => d.AlertId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AlertSegments_DisasterAlerts");
        });

        modelBuilder.Entity<AlertsTrail>(entity =>
        {
            entity.HasKey(e => e.AlertTrailId);

            entity.HasIndex(e => new { e.AlertId, e.TrailId }, "UQ_AlertsTrails_AlertId_TrailId").IsUnique();

            entity.Property(e => e.AlertTrailId).HasColumnName("AlertTrail_Id");
            entity.Property(e => e.ReasonDescription)
                .HasMaxLength(2000)
                .HasColumnName("Reason_Description");
            entity.Property(e => e.TrailId).HasColumnName("Trail_Id");

            entity.HasOne(d => d.Alert).WithMany(p => p.AlertsTrails)
                .HasForeignKey(d => d.AlertId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AlertsTrails_Alert_Id");

            entity.HasOne(d => d.Trail).WithMany(p => p.AlertsTrails)
                .HasForeignKey(d => d.TrailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AlertsTrails_Trail_Id");
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PK__Announce__853AB7CFB653CA76");

            entity.ToTable("Announcement");

            entity.Property(e => e.AnnouncementId).HasColumnName("Announcement_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("Article");

            entity.Property(e => e.ArticleId).HasColumnName("Article_ID");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Category).WithMany(p => p.Articles)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Article_Category");

            entity.HasOne(d => d.User).WithMany(p => p.Articles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Article_User");

            entity.HasMany(d => d.Tags).WithMany(p => p.Articles)
                .UsingEntity<Dictionary<string, object>>(
                    "ArticleTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ArticleTag_Tag"),
                    l => l.HasOne<Article>().WithMany()
                        .HasForeignKey("ArticleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ArticleTag_Article"),
                    j =>
                    {
                        j.HasKey("ArticleId", "TagId");
                        j.ToTable("ArticleTag");

                        j.IndexerProperty<int>("ArticleId")
                            .HasColumnName("Article_ID");

                        j.IndexerProperty<int>("TagId")
                            .HasColumnName("Tag_ID");
                    });
        });

        modelBuilder.Entity<ArticleImage>(entity =>
        {
            entity.HasKey(e => e.ImageId);

            entity.ToTable("ArticleImage");

            entity.Property(e => e.ImageId).HasColumnName("Image_ID");
            entity.Property(e => e.ArticleId).HasColumnName("Article_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImagePath).HasMaxLength(255);
            entity.Property(e => e.SortOrder).HasDefaultValue(1);

            entity.HasOne(d => d.Article).WithMany(p => p.ArticleImages)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticleImage_Article");
        });

        modelBuilder.Entity<ArticleLike>(entity =>
        {
            entity.HasKey(e => e.LikeId);

            entity.ToTable("ArticleLike");

            entity.HasIndex(e => new { e.UserId, e.ArticleId }, "UQ_ArticleLike").IsUnique();

            entity.Property(e => e.LikeId).HasColumnName("Like_ID");
            entity.Property(e => e.ArticleId).HasColumnName("Article_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Article).WithMany(p => p.ArticleLikes)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticleLike_Article");

            entity.HasOne(d => d.User).WithMany(p => p.ArticleLikes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticleLike_User");
        });

        modelBuilder.Entity<ArticleView>(entity =>
        {
            entity.HasKey(e => e.ViewId);

            entity.ToTable("ArticleView");

            entity.Property(e => e.ViewId)
                .HasColumnName("View_ID");

            entity.Property(e => e.ArticleId)
                .HasColumnName("Article_ID");

            entity.Property(e => e.UserId)
                .HasColumnName("User_ID");

            entity.Property(e => e.ViewedDate)
                .HasDefaultValueSql("(getdate())", "DF_ArticleView_ViewedDate")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Article)
                .WithMany(p => p.ArticleViews)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticleView_Article");

            entity.HasOne(d => d.User)
                .WithMany(p => p.ArticleViews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ArticleView_User");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.CategoryName).HasMaxLength(30);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comment");

            entity.Property(e => e.CommentId).HasColumnName("Comment_ID");
            entity.Property(e => e.ArticleId).HasColumnName("Article_ID");
            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ParentCommentId).HasColumnName("ParentComment_ID");
            entity.Property(e => e.ReplyToUserId).HasColumnName("ReplyToUser_ID");
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Article).WithMany(p => p.Comments)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comment_Article");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .HasConstraintName("FK_Comment_Parent");

            entity.HasOne(d => d.ReplyToUser).WithMany(p => p.CommentReplyToUsers)
                .HasForeignKey(d => d.ReplyToUserId)
                .HasConstraintName("FK_Comment_ReplyUser");

            entity.HasOne(d => d.User).WithMany(p => p.CommentUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comment_User");
        });

        modelBuilder.Entity<CommentImage>(entity =>
        {
            entity.HasKey(e => e.ImageId);

            entity.ToTable("CommentImage");

            entity.Property(e => e.ImageId).HasColumnName("Image_ID");
            entity.Property(e => e.CommentId).HasColumnName("Comment_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImagePath).HasMaxLength(255);

            entity.HasOne(d => d.Comment).WithMany(p => p.CommentImages)
                .HasForeignKey(d => d.CommentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CommentImage_Comment");
        });

        modelBuilder.Entity<DisasterAlert>(entity =>
        {
            entity.HasKey(e => e.AlertId);

            entity.Property(e => e.AlertDescription).HasMaxLength(2000);
            entity.Property(e => e.AlertTitle).HasMaxLength(180);
            entity.Property(e => e.AlertType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.EffectiveFrom).HasPrecision(0);
            entity.Property(e => e.EffectiveTo).HasPrecision(0);
            entity.Property(e => e.SourceAgency).HasMaxLength(150);
            entity.Property(e => e.SourceUrl).HasMaxLength(1000);
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EquipmentName).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RequirementLevel).HasMaxLength(20);

            entity.HasOne(d => d.Category).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Equipments_CategoryId");
        });

        modelBuilder.Entity<EquipmentCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.Property(e => e.CategoryName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<EventData>(entity =>
        {
            entity.HasKey(e => e.EventId);

            entity.ToTable("Event_Data");

            entity.Property(e => e.EventId).HasColumnName("Event_Id");
            entity.Property(e => e.ActivityPhoto)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Activity_Photo");
            entity.Property(e => e.ActivityStatus)
                .HasMaxLength(10)
                .HasColumnName("Activity_Status");
            entity.Property(e => e.EventDate)
                .HasColumnType("datetime")
                .HasColumnName("Event_Date");

            entity.Property(e => e.EventStartTime)
                .HasColumnType("datetime")
                .HasColumnName("Event_Start_Time");

            entity.Property(e => e.EventEndTime)
                .HasColumnType("datetime")
                .HasColumnName("Event_End_Time");
            entity.Property(e => e.EventName)
                .HasMaxLength(50)
                .HasColumnName("Event_Name");
            entity.Property(e => e.HasActiveReport).HasColumnName("Has_Active_Report");
            entity.Property(e => e.LeaderUserId).HasColumnName("Leader_User_Id");
            entity.Property(e => e.MaximumNumber).HasColumnName("Maximum_Number");
            entity.Property(e => e.MountainId).HasColumnName("Mountain_Id");
            entity.Property(e => e.ReviewRequired).HasColumnName("Review_Required");
            entity.Property(e => e.ReviewStatus)
                .HasMaxLength(10)
                .HasColumnName("Review_Status");

            entity.HasOne(d => d.LeaderUser).WithMany(p => p.EventData)
                .HasForeignKey(d => d.LeaderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventData_LeaderUser");

            entity.HasOne(d => d.Mountain).WithMany(p => p.EventData)
                .HasForeignKey(d => d.MountainId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_Data_Mountain_Id");
        });

        modelBuilder.Entity<EventLeaderRating>(entity =>
        {
            entity.HasKey(e => e.ReviewId);

            entity.ToTable("EventLeader_Rating");

            entity.Property(e => e.ReviewId).HasColumnName("Review_Id");
            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("Created_At");
            entity.Property(e => e.EventId).HasColumnName("Event_Id");
            entity.Property(e => e.LeaderRating).HasColumnName("Leader_Rating");
            entity.Property(e => e.UserId).HasColumnName("User_Id");

            entity.HasOne(d => d.Event).WithMany(p => p.EventLeaderRatings)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventLeader_Rating_Event_Id");

            entity.HasOne(d => d.User).WithMany(p => p.EventLeaderRatings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventLeader_Rating_User_Id");
        });

        modelBuilder.Entity<EventRegistrationAndMemberList>(entity =>
        {
            entity.HasKey(e => e.SignUpId);

            entity.ToTable("Event_Registration_and_Member_List");

            entity.Property(e => e.SignUpId).HasColumnName("Sign_Up_Id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("Created_At");
            entity.Property(e => e.EmergencyContact)
                .HasMaxLength(100)
                .HasColumnName("Emergency_Contact");
            entity.Property(e => e.EventId).HasColumnName("Event_Id");
            entity.Property(e => e.RegistrationStatus).HasColumnName("Registration_Status");
            entity.Property(e => e.UserId).HasColumnName("User_Id");

            entity.HasOne(d => d.Event).WithMany(p => p.EventRegistrationAndMemberLists)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_Registration_and_Member_List_Event_Id");

            entity.HasOne(d => d.User).WithMany(p => p.EventRegistrationAndMemberLists)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_Registration_and_Member_List_User_Id");
        });

        modelBuilder.Entity<EventReportComplaint>(entity =>
        {
            entity.HasKey(e => e.ReportEventId);

            entity.ToTable("Event_Report_Complaint");

            entity.Property(e => e.ReportEventId).HasColumnName("Report_Event_Id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("Created_At");
            entity.Property(e => e.EventId).HasColumnName("Event_Id");
            entity.Property(e => e.EvidenceUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Evidence_Url");
            entity.Property(e => e.ReportReason)
                .HasMaxLength(500)
                .HasColumnName("Report_Reason");
            entity.Property(e => e.ReportStatus)
                .HasMaxLength(10)
                .HasColumnName("Report_Status");
            entity.Property(e => e.UserId).HasColumnName("User_Id");
            entity.Property(e => e.ReportTitle)
    .HasMaxLength(100)
    .HasColumnName("Report_Title");


            entity.HasOne(d => d.Event).WithMany(p => p.EventReportComplaints)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_Report_Complaint_Event_Id");

            entity.HasOne(d => d.User).WithMany(p => p.EventReportComplaints)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_Report_Complaint_User_Id");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.ToTable("Favorite");

            entity.HasIndex(e => new { e.UserId, e.ArticleId }, "UQ_Favorite").IsUnique();

            entity.Property(e => e.FavoriteId).HasColumnName("Favorite_ID");
            entity.Property(e => e.ArticleId).HasColumnName("Article_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Article).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favorite_Article");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favorite_User");
        });

        modelBuilder.Entity<GroupAttendance>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.UserId });

            entity.ToTable("group_attendance");

            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AttendanceStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("attendance_status");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupAttendances)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_group_attendance_group_id");

            entity.HasOne(d => d.User).WithMany(p => p.GroupAttendances)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_group_attendance_user_id");
        });

        modelBuilder.Entity<HikeRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId);

            entity.ToTable("hike_records");

            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.CompanionCount).HasColumnName("companion_count");
            entity.Property(e => e.HikeDate).HasColumnName("hike_date");
            entity.Property(e => e.MountainId).HasColumnName("mountain_id");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("note");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Verified).HasColumnName("verified");

            entity.HasOne(d => d.Mountain).WithMany(p => p.HikeRecords)
                .HasForeignKey(d => d.MountainId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_hike_records_mountain_id");

            entity.HasOne(d => d.User).WithMany(p => p.HikeRecords)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_hike_records_user_id");
        });

        modelBuilder.Entity<HikeRecordDetail>(entity =>
        {
            entity.HasKey(e => e.HikerecDetailId);

            entity.ToTable("hike_record_details");

            entity.Property(e => e.HikerecDetailId).HasColumnName("hikerec_detail_id");
            entity.Property(e => e.CalculatedRiskScore).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.HikeRecordId).HasColumnName("hike_record_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TrailId).HasColumnName("Trail_Id");

            entity.HasOne(d => d.HikeRecord).WithMany(p => p.HikeRecordDetails)
                .HasForeignKey(d => d.HikeRecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_hike_record_details_hike_record_id");

            entity.HasOne(d => d.Trail).WithMany(p => p.HikeRecordDetails)
                .HasForeignKey(d => d.TrailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_hike_record_details_Trail_Id");
        });

        modelBuilder.Entity<Indicator>(entity =>
        {
            entity.Property(e => e.DataSource).HasMaxLength(500);
            entity.Property(e => e.IndicatorDescription).HasMaxLength(1500);
            entity.Property(e => e.IndicatorName).HasMaxLength(120);
            entity.Property(e => e.IndicatorType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Indicators_IsActive");
            entity.Property(e => e.Weight).HasColumnType("decimal(6, 3)");
        });

        modelBuilder.Entity<IndicatorSegment>(entity =>
        {
            entity.HasIndex(e => e.IndicatorId, "IX_IndicatorSegments_IndicatorId");

            entity.HasIndex(e => e.Shape, "SIX_IndicatorSegments_Shape");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SegmentName).HasMaxLength(200);
            entity.Property(e => e.SourceFeatureId).HasMaxLength(100);

            entity.HasOne(d => d.Indicator).WithMany(p => p.IndicatorSegments)
                .HasForeignKey(d => d.IndicatorId)
                .HasConstraintName("FK_IndicatorSegments_Indicators");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.ToTable("levels");

            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.LevelName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("level_name");
            entity.Property(e => e.MaxXp).HasColumnName("max_xp");
            entity.Property(e => e.MinXp).HasColumnName("min_xp");
        });

        modelBuilder.Entity<Mountain>(entity =>
        {
            entity.Property(e => e.MountainId).HasColumnName("Mountain_Id");
            entity.Property(e => e.DifficultyLevel).HasColumnName("Difficulty_Level");
            entity.Property(e => e.Location).HasMaxLength(50);
            entity.Property(e => e.MountainName)
                .HasMaxLength(100)
                .HasColumnName("Mountain_Name");
            entity.Property(e => e.MountainsPermitRequired).HasColumnName("Mountains_Permit_Required");
            entity.Property(e => e.NationalParkPermitRequired).HasColumnName("National_Park_Permit_Required");
            entity.Property(e => e.Longitude).HasPrecision(9, 6);
            entity.Property(e => e.Latitude).HasPrecision(9, 6);
        });

        modelBuilder.Entity<MountainEquipmentSuggestion>(entity =>
        {
            entity.HasKey(e => e.SuggestionId);

            entity.Property(e => e.ExperienceLevel).HasMaxLength(20);
            entity.Property(e => e.IntensityLevel).HasMaxLength(20);
            entity.Property(e => e.MinimumDays).HasDefaultValue(1);
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.RequirementLevel).HasMaxLength(20);
            entity.Property(e => e.Season).HasMaxLength(20);
            entity.Property(e => e.SuggestedQuantity).HasDefaultValue(1);

            entity.HasOne(d => d.Equipment).WithMany(p => p.MountainEquipmentSuggestions)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MountainEquipmentSuggestions_EquipmentId");

            entity.HasOne(d => d.Mountain).WithMany(p => p.MountainEquipmentSuggestions)
                .HasForeignKey(d => d.MountainId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MountainEquipmentSuggestions_MountainId");
        });

        modelBuilder.Entity<Notify>(entity =>
        {
            entity.HasKey(e => e.NotificationId);

            entity.ToTable("Notify");

            entity.Property(e => e.NotificationId).HasColumnName("Notification_Id");
            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("Created_At");
            entity.Property(e => e.IsRead).HasColumnName("Is_Read");
            entity.Property(e => e.RelatedFormType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Related_FormType");
            entity.Property(e => e.RelatedId).HasColumnName("Related_Id");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("User_Id");
            entity.Property(e => e.UsingPipeline)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifies)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notify_User_Id");
        });


        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");

            entity.Property(e => e.NotificationId)
                .HasColumnName("Notification_ID");

            entity.Property(e => e.UserId)
                .HasColumnName("User_ID");

            entity.Property(e => e.SenderUserId)
                .HasColumnName("Sender_User_ID");

            entity.Property(e => e.ArticleId)
                .HasColumnName("Article_ID");

            entity.Property(e => e.CommentId)
                .HasColumnName("Comment_ID");

            entity.Property(e => e.Message)
                .HasMaxLength(300);

            entity.Property(e => e.IsRead)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())", "DF_Notification_CreatedDate")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Article)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ArticleId)
                .HasConstraintName("FK_Notification_Article");

            entity.HasOne(d => d.Comment)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.CommentId)
                .HasConstraintName("FK_Notification_Comment");

            entity.HasOne(d => d.SenderUser)
                .WithMany(p => p.NotificationSenderUsers)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_SenderUser");

            entity.HasOne(d => d.User)
                .WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_User");
        });
        modelBuilder.Entity<PersonalEquipmentDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId);

            entity.Property(e => e.CustomEquipmentName).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.RequirementLevel).HasMaxLength(20);

            entity.HasOne(d => d.Equipment).WithMany(p => p.PersonalEquipmentDetails)
                .HasForeignKey(d => d.EquipmentId)
                .HasConstraintName("FK_PersonalEquipmentDetails_EquipmentId");

            entity.HasOne(d => d.List).WithMany(p => p.PersonalEquipmentDetails)
                .HasForeignKey(d => d.ListId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PersonalEquipmentDetails_ListId");
        });

        modelBuilder.Entity<PersonalEquipmentList>(entity =>
        {
            entity.HasKey(e => e.ListId);

            entity.Property(e => e.BodyWeightKg).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ExperienceLevel).HasMaxLength(20);
            entity.Property(e => e.IntensityLevel).HasMaxLength(20);
            entity.Property(e => e.ListName).HasMaxLength(100);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Season).HasMaxLength(20);
            entity.Property(e => e.WeightPercentage).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.WeightStatus).HasMaxLength(20);

            entity.HasOne(d => d.Member).WithMany(p => p.PersonalEquipmentLists)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PersonalEquipmentLists_MemberId");

            entity.HasOne(d => d.Mountain).WithMany(p => p.PersonalEquipmentLists)
                .HasForeignKey(d => d.MountainId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PersonalEquipmentLists_MountainId");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("Report");

            entity.Property(e => e.ReportId).HasColumnName("Report_ID");
            entity.Property(e => e.AdminId).HasColumnName("Admin_ID");
            entity.Property(e => e.ArticleId).HasColumnName("Article_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(300);
            entity.Property(e => e.Reply).HasMaxLength(300);
            entity.Property(e => e.ReviewDate).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Admin).WithMany(p => p.ReportAdmins)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("FK_Report_Admin");

            entity.HasOne(d => d.Article).WithMany(p => p.Reports)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Report_Article");

            entity.HasOne(d => d.User).WithMany(p => p.ReportUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Report_User");
        });

        modelBuilder.Entity<ReviewApplication>(entity =>
        {
            entity.HasKey(e => e.ApplicationId);

            entity.Property(e => e.ApplicationType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.Purpose).HasMaxLength(1000);
            entity.Property(e => e.RelatedHikeDrecordId).HasColumnName("Related_Hike_DRecord_Id");
            entity.Property(e => e.ReviewNote).HasMaxLength(1500);
            entity.Property(e => e.ReviewedAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ApplicantUser).WithMany(p => p.ReviewApplicationApplicantUsers)
                .HasForeignKey(d => d.ApplicantUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewApplications_ApplicantUserId");

            entity.HasOne(d => d.RelatedHikeDrecord).WithMany(p => p.ReviewApplications)
                .HasForeignKey(d => d.RelatedHikeDrecordId)
                .HasConstraintName("FK_ReviewApplications_Related_Hike_DRecord_Id");

            entity.HasOne(d => d.RelatedReport).WithMany(p => p.ReviewApplications)
                .HasForeignKey(d => d.RelatedReportId)
                .HasConstraintName("FK_ReviewApplications_RelatedReportId");

            entity.HasOne(d => d.ReviewerUser).WithMany(p => p.ReviewApplicationReviewerUsers)
                .HasForeignKey(d => d.ReviewerUserId)
                .HasConstraintName("FK_ReviewApplications_ReviewerUserId");
        });

        modelBuilder.Entity<SkillTag>(entity =>
        {
            entity.HasKey(e => e.TagId);

            entity.ToTable("skill_tags");

            entity.Property(e => e.TagId).HasColumnName("tag_id");
            entity.Property(e => e.Category)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("category");
            entity.Property(e => e.ParentTagId).HasColumnName("parent_tag_id");
            entity.Property(e => e.TagName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tag_name");
            entity.Property(e => e.UnlockCondition)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("unlock_condition");

            entity.HasOne(d => d.ParentTag).WithMany(p => p.InverseParentTag)
                .HasForeignKey(d => d.ParentTagId)
                .HasConstraintName("FK_skill_tags_parent_tag_id");
        });

        modelBuilder.Entity<SuspensionSchedule>(entity =>
        {
            entity.HasKey(e => e.BanId);

            entity.ToTable("Suspension_Schedule");

            entity.Property(e => e.BanId).HasColumnName("Ban_Id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("Created_At");
            entity.Property(e => e.EventId).HasColumnName("Event_Id");
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.SuspensionExpirationTime)
                .HasColumnType("datetime")
                .HasColumnName("Suspension_Expiration_Time");
            entity.Property(e => e.SuspensionStatus)
                .HasMaxLength(10)
                .HasColumnName("Suspension_Status");
            entity.Property(e => e.UserId).HasColumnName("User_Id");

            entity.HasOne(d => d.Event).WithMany(p => p.SuspensionSchedules)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK_Suspension_Schedule_Event_Id");

            entity.HasOne(d => d.User).WithMany(p => p.SuspensionSchedules)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suspension_Schedule_User_Id");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tag");

            entity.HasIndex(e => e.TagName, "UQ_Tag_TagName").IsUnique();

            entity.Property(e => e.TagId).HasColumnName("Tag_ID");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())", "DF_Tag_CreatedDate")
                .HasColumnType("datetime");

            entity.Property(e => e.TagName)
                .HasMaxLength(30);
        });

        modelBuilder.Entity<Trail>(entity =>
        {
            entity.Property(e => e.TrailId).HasColumnName("Trail_Id");
            entity.Property(e => e.DifficultyLevel).HasColumnName("Difficulty_Level");
            entity.Property(e => e.DistanceKm)
                .HasColumnType("decimal(7, 2)")
                .HasColumnName("Distance_Km");
            entity.Property(e => e.EstimatedHours).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.GuideRequired).HasColumnName("Guide_Required");
            entity.Property(e => e.PermitRequired).HasColumnName("Permit_Required");
            entity.Property(e => e.Region).HasMaxLength(80);
            entity.Property(e => e.RegulationNote).HasMaxLength(1000);
            entity.Property(e => e.TrailName)
                .HasMaxLength(120)
                .HasColumnName("Trail_Name");
        });

        modelBuilder.Entity<TrailFeature>(entity =>
        {
            entity.HasKey(e => e.FeatureId);

            entity.Property(e => e.DataSource).HasMaxLength(200);
            entity.Property(e => e.FeatureDescription).HasMaxLength(1000);
            entity.Property(e => e.FeatureName).HasMaxLength(120);
            entity.Property(e => e.FeatureType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TrailId).HasColumnName("Trail_Id");

            entity.HasOne(d => d.Trail).WithMany(p => p.TrailFeatures)
                .HasForeignKey(d => d.TrailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrailFeatures_Trail_Id");
        });

        modelBuilder.Entity<TrailIndicator>(entity =>
        {
            entity.HasKey(e => new { e.TrailId, e.IndicatorId });

            entity.HasIndex(e => e.IndicatorId, "IX_TrailIndicators_IndicatorId");

            entity.Property(e => e.TrailId).HasColumnName("Trail_Id");
            entity.Property(e => e.DistanceMeters).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.EvaluatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_TrailIndicators_EvaluatedAt");
            entity.Property(e => e.EvaluatedScore).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.IndicatorWeightSnapshot).HasColumnType("decimal(6, 3)");
            entity.Property(e => e.OverlapRatio).HasColumnType("decimal(7, 6)");
            entity.Property(e => e.RawScore).HasColumnType("decimal(10, 4)");

            entity.HasOne(d => d.Indicator).WithMany(p => p.TrailIndicators)
                .HasForeignKey(d => d.IndicatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrailIndicators_Indicators");

            entity.HasOne(d => d.Trail).WithMany(p => p.TrailIndicators)
                .HasForeignKey(d => d.TrailId)
                .HasConstraintName("FK_TrailIndicators_Trails");
        });

        modelBuilder.Entity<TrailSegment>(entity =>
        {
            entity.HasIndex(e => e.Shape, "SIX_TrailSegments_Shape");

            entity.Property(e => e.TrailSegmentId).HasColumnName("TrailSegment_Id");
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.SourceId)
                .HasMaxLength(100)
                .HasColumnName("Source_Id");
            entity.Property(e => e.SourceUrl)
                .HasMaxLength(500)
                .HasColumnName("Source_Url");
            entity.Property(e => e.TrailId).HasColumnName("Trail_Id");

            entity.HasOne(d => d.Trail).WithMany(p => p.TrailSegments)
                .HasForeignKey(d => d.TrailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrailSegments_Trails");
        });

        modelBuilder.Entity<TrailSubscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);

            entity.HasIndex(e => new { e.UserId, e.TrailId }, "UQ_TrailSubscriptions_UserId_TrailId").IsUnique();

            entity.Property(e => e.TrailId).HasColumnName("Trail_Id");

            entity.HasOne(d => d.Trail).WithMany(p => p.TrailSubscriptions)
                .HasForeignKey(d => d.TrailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrailSubscriptions_Trail_Id");

            entity.HasOne(d => d.User).WithMany(p => p.TrailSubscriptions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrailSubscriptions_UserId");
        });

        modelBuilder.Entity<TripReport>(entity =>
        {
            entity.HasKey(e => e.ReportId);

            entity.Property(e => e.OccurredAt).HasPrecision(0);
            entity.Property(e => e.ReportContent).HasMaxLength(3000);
            entity.Property(e => e.ReportType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ReviewStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ReviewedAt).HasPrecision(0);
            entity.Property(e => e.SourceType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TrailId).HasColumnName("Trail_Id");

            entity.HasOne(d => d.ReporterUser).WithMany(p => p.TripReportReporterUsers)
                .HasForeignKey(d => d.ReporterUserId)
                .HasConstraintName("FK_TripReports_ReporterUserId");

            entity.HasOne(d => d.ReviewedByUser).WithMany(p => p.TripReportReviewedByUsers)
                .HasForeignKey(d => d.ReviewedByUserId)
                .HasConstraintName("FK_TripReports_ReviewedByUserId");

            entity.HasOne(d => d.Trail).WithMany(p => p.TripReports)
                .HasForeignKey(d => d.TrailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TripReports_TrId");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E616496713F0A").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("account_status");
            entity.Property(e => e.AvatarBlurState)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("avatar_blur_state");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.Bio)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentLevelId).HasColumnName("current_level_id");
            entity.Property(e => e.DisplayedAchievementId).HasColumnName("displayed_achievement_id");
            entity.Property(e => e.DifficultyPreference)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("difficulty_preference");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.LastActiveAt)
                .HasColumnType("datetime")
                .HasColumnName("last_active_at");
            entity.Property(e => e.Nickname)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("nickname");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.RegionPreference)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("region_preference");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.TotalXp).HasColumnName("total_xp");

            entity.HasOne(d => d.CurrentLevel).WithMany(p => p.Users)
                .HasForeignKey(d => d.CurrentLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_users_current_level_id");

            entity.HasOne(d => d.DisplayedAchievement).WithMany()
                .HasForeignKey(d => d.DisplayedAchievementId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_users_displayed_achievement_id");
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.AchievementId });

            entity.ToTable("user_achievements");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AchievementId).HasColumnName("achievement_id");
            entity.Property(e => e.UnlockedAt)
                .HasColumnType("datetime")
                .HasColumnName("unlocked_at");

            entity.HasOne(d => d.Achievement).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.AchievementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_achievements_achievement_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_achievements_user_id");
        });

        modelBuilder.Entity<UserSkillTag>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.TagId });

            entity.ToTable("user_skill_tags");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TagId).HasColumnName("tag_id");
            entity.Property(e => e.IsDisplayed).HasColumnName("is_displayed");
            entity.Property(e => e.Source)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("source");

            entity.HasOne(d => d.SkillTag).WithMany(p => p.UserSkillTags)
                .HasForeignKey(d => d.TagId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_skill_tags_tag_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserSkillTags)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_skill_tags_user_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
