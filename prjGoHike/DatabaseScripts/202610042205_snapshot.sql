CREATE TABLE [dbo].[TrailSubscriptions] (
    [SubscriptionId]      BIGINT IDENTITY (1, 1) NOT NULL,
    [UserId]              BIGINT NOT NULL,
    [Trail_Id]            BIGINT NOT NULL,
    [NotifyLegalChange]   BIT    NOT NULL,
    [NotifyDisasterAlert] BIT    NOT NULL,
    [NotifyNewReport]     BIT    NOT NULL,
    [IsActive]            BIT    NOT NULL,
    CONSTRAINT [PK_TrailSubscriptions] PRIMARY KEY CLUSTERED ([SubscriptionId] ASC),
    CONSTRAINT [FK_TrailSubscriptions_Trail_Id] FOREIGN KEY ([Trail_Id]) REFERENCES [dbo].[Trails] ([Trail_Id]),
    CONSTRAINT [FK_TrailSubscriptions_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [UQ_TrailSubscriptions_UserId_TrailId] UNIQUE NONCLUSTERED ([UserId] ASC, [Trail_Id] ASC)
);


GO

CREATE TABLE [dbo].[hike_record_details] (
    [hikerec_detail_id]   BIGINT            IDENTITY (1, 1) NOT NULL,
    [hike_record_id]      BIGINT            NOT NULL,
    [Trail_Id]            BIGINT            NOT NULL,
    [StartDate]           DATE              NOT NULL,
    [EndDate]             DATE              NOT NULL,
    [Status]              VARCHAR (20)      NOT NULL,
    [UploadedTrack]       [sys].[geography] NULL,
    [CalculatedRiskScore] DECIMAL (6, 2)    NULL,
    CONSTRAINT [PK_hike_record_details] PRIMARY KEY CLUSTERED ([hikerec_detail_id] ASC),
    CONSTRAINT [FK_hike_record_details_hike_record_id] FOREIGN KEY ([hike_record_id]) REFERENCES [dbo].[hike_records] ([record_id]),
    CONSTRAINT [FK_hike_record_details_Trail_Id] FOREIGN KEY ([Trail_Id]) REFERENCES [dbo].[Trails] ([Trail_Id])
);


GO

CREATE TABLE [dbo].[Tag] (
    [Tag_ID]      INT           IDENTITY (1, 1) NOT NULL,
    [TagName]     NVARCHAR (30) NOT NULL,
    [CreatedDate] DATETIME      CONSTRAINT [DF_Tag_CreatedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Tag] PRIMARY KEY CLUSTERED ([Tag_ID] ASC),
    CONSTRAINT [UQ_Tag_TagName] UNIQUE NONCLUSTERED ([TagName] ASC)
);


GO

CREATE TABLE [dbo].[ReviewApplications] (
    [ApplicationId]           BIGINT          IDENTITY (1, 1) NOT NULL,
    [ApplicantUserId]         BIGINT          NOT NULL,
    [ApplicationType]         VARCHAR (20)    NOT NULL,
    [Related_Hike_DRecord_Id] BIGINT          NULL,
    [RelatedReportId]         BIGINT          NULL,
    [Purpose]                 NVARCHAR (1000) NOT NULL,
    [RequestPayloadJson]      NVARCHAR (MAX)  NULL,
    [RequestedPoints]         INT             NULL,
    [Status]                  VARCHAR (20)    NOT NULL,
    [ReviewerUserId]          BIGINT          NULL,
    [ReviewNote]              NVARCHAR (1500) NULL,
    [CreatedAt]               DATETIME2 (0)   NOT NULL,
    [ReviewedAt]              DATETIME2 (0)   NULL,
    CONSTRAINT [PK_ReviewApplications] PRIMARY KEY CLUSTERED ([ApplicationId] ASC),
    CONSTRAINT [FK_ReviewApplications_ApplicantUserId] FOREIGN KEY ([ApplicantUserId]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [FK_ReviewApplications_Related_Hike_DRecord_Id] FOREIGN KEY ([Related_Hike_DRecord_Id]) REFERENCES [dbo].[hike_record_details] ([hikerec_detail_id]),
    CONSTRAINT [FK_ReviewApplications_RelatedReportId] FOREIGN KEY ([RelatedReportId]) REFERENCES [dbo].[TripReports] ([ReportId]),
    CONSTRAINT [FK_ReviewApplications_ReviewerUserId] FOREIGN KEY ([ReviewerUserId]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Article] (
    [Article_ID]  INT            IDENTITY (1, 1) NOT NULL,
    [User_ID]     BIGINT         NOT NULL,
    [Category_ID] INT            NOT NULL,
    [Title]       NVARCHAR (100) NOT NULL,
    [Content]     NVARCHAR (MAX) NOT NULL,
    [CreatedDate] DATETIME       DEFAULT (getdate()) NOT NULL,
    [UpdateDate]  DATETIME       NULL,
    [Status]      TINYINT        DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Article] PRIMARY KEY CLUSTERED ([Article_ID] ASC),
    CONSTRAINT [FK_Article_Category] FOREIGN KEY ([Category_ID]) REFERENCES [dbo].[Category] ([Category_ID]),
    CONSTRAINT [FK_Article_User] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Notification] (
    [Notification_ID] BIGINT         IDENTITY (1, 1) NOT NULL,
    [User_ID]         BIGINT         NOT NULL,
    [Sender_User_ID]  BIGINT         NOT NULL,
    [Article_ID]      INT            NULL,
    [Comment_ID]      INT            NULL,
    [Type]            TINYINT        NOT NULL,
    [Message]         NVARCHAR (300) NOT NULL,
    [IsRead]          BIT            CONSTRAINT [DF_Notification_IsRead] DEFAULT ((0)) NOT NULL,
    [CreatedDate]     DATETIME       CONSTRAINT [DF_Notification_CreatedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Notification] PRIMARY KEY CLUSTERED ([Notification_ID] ASC),
    CONSTRAINT [FK_Notification_Article] FOREIGN KEY ([Article_ID]) REFERENCES [dbo].[Article] ([Article_ID]),
    CONSTRAINT [FK_Notification_Comment] FOREIGN KEY ([Comment_ID]) REFERENCES [dbo].[Comment] ([Comment_ID]),
    CONSTRAINT [FK_Notification_SenderUser] FOREIGN KEY ([Sender_User_ID]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [FK_Notification_User] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Event_Registration_and_Member_List] (
    [Sign_Up_Id]          BIGINT         IDENTITY (1, 1) NOT NULL,
    [User_Id]             BIGINT         NOT NULL,
    [Event_Id]            BIGINT         NOT NULL,
    [Registration_Status] INT            NOT NULL,
    [Emergency_Contact]   NVARCHAR (100) NOT NULL,
    [Created_At]          DATETIME       NOT NULL,
    [Cancelled_At]        DATETIME       NULL,
    CONSTRAINT [PK_Event_Registration_and_Member_List] PRIMARY KEY CLUSTERED ([Sign_Up_Id] ASC),
    CONSTRAINT [FK_Event_Registration_and_Member_List_Event_Id] FOREIGN KEY ([Event_Id]) REFERENCES [dbo].[Event_Data] ([Event_Id]),
    CONSTRAINT [FK_Event_Registration_and_Member_List_User_Id] FOREIGN KEY ([User_Id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Trails] (
    [Trail_Id]         BIGINT          IDENTITY (1, 1) NOT NULL,
    [Trail_Name]       NVARCHAR (120)  NOT NULL,
    [Region]           NVARCHAR (80)   NOT NULL,
    [Difficulty_Level] INT             NOT NULL,
    [Distance_Km]      DECIMAL (7, 2)  NULL,
    [EstimatedHours]   DECIMAL (6, 2)  NULL,
    [Permit_Required]  BIT             NOT NULL,
    [Guide_Required]   BIT             NOT NULL,
    [RegulationNote]   NVARCHAR (1000) NULL,
    [IsPublished]      BIT             NOT NULL,
    CONSTRAINT [PK_Trails] PRIMARY KEY CLUSTERED ([Trail_Id] ASC)
);


GO

CREATE TABLE [dbo].[PersonalEquipmentLists] (
    [ListId]              BIGINT         IDENTITY (1, 1) NOT NULL,
    [MemberId]            BIGINT         NOT NULL,
    [MountainId]          BIGINT         NOT NULL,
    [ListName]            NVARCHAR (100) NOT NULL,
    [HikingDate]          DATE           NOT NULL,
    [HikingDays]          INT            NOT NULL,
    [Season]              NVARCHAR (20)  NOT NULL,
    [IntensityLevel]      NVARCHAR (20)  NULL,
    [ExperienceLevel]     NVARCHAR (20)  NULL,
    [BodyWeightKg]        DECIMAL (5, 2) NOT NULL,
    [MaxCarryWeightGram]  INT            NOT NULL,
    [TotalWeightGram]     INT            DEFAULT ((0)) NOT NULL,
    [RemainingWeightGram] INT            DEFAULT ((0)) NOT NULL,
    [WeightPercentage]    DECIMAL (6, 2) NOT NULL,
    [WeightStatus]        NVARCHAR (20)  NOT NULL,
    [IsDeleted]           BIT            DEFAULT ((0)) NOT NULL,
    [CreatedAt]           DATETIME2 (7)  NOT NULL,
    [UpdatedAt]           DATETIME2 (7)  NULL,
    [RowVersion]          ROWVERSION     NOT NULL,
    CONSTRAINT [PK_PersonalEquipmentLists] PRIMARY KEY CLUSTERED ([ListId] ASC),
    CONSTRAINT [CK_PersonalEquipmentLists_HikingDays] CHECK ([HikingDays]>(0)),
    CONSTRAINT [CK_PersonalEquipmentLists_MaxCarryWeightGram] CHECK ([MaxCarryWeightGram]>(0)),
    CONSTRAINT [FK_PersonalEquipmentLists_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [FK_PersonalEquipmentLists_MountainId] FOREIGN KEY ([MountainId]) REFERENCES [dbo].[Mountains] ([Mountain_Id])
);


GO

CREATE TABLE [dbo].[DisasterAlerts] (
    [AlertId]          BIGINT          IDENTITY (1, 1) NOT NULL,
    [AlertType]        VARCHAR (30)    NOT NULL,
    [AlertTitle]       NVARCHAR (180)  NOT NULL,
    [AlertDescription] NVARCHAR (2000) NULL,
    [SeverityLevel]    TINYINT         NOT NULL,
    [EffectiveFrom]    DATETIME2 (0)   NOT NULL,
    [EffectiveTo]      DATETIME2 (0)   NULL,
    [SourceAgency]     NVARCHAR (150)  NULL,
    [SourceUrl]        NVARCHAR (1000) NULL,
    [IsActive]         BIT             NOT NULL,
    CONSTRAINT [PK_DisasterAlerts] PRIMARY KEY CLUSTERED ([AlertId] ASC),
    CONSTRAINT [CK_DisasterAlerts_EffectivePeriod] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo]>=[EffectiveFrom]),
    CONSTRAINT [CK_DisasterAlerts_SeverityLevel] CHECK ([SeverityLevel]>=(1) AND [SeverityLevel]<=(5))
);


GO

CREATE TABLE [dbo].[Category] (
    [Category_ID]  INT           IDENTITY (1, 1) NOT NULL,
    [CategoryName] NVARCHAR (30) NOT NULL,
    CONSTRAINT [PK_Category] PRIMARY KEY CLUSTERED ([Category_ID] ASC)
);


GO

CREATE TABLE [dbo].[group_attendance] (
    [group_id]          BIGINT       NOT NULL,
    [user_id]           BIGINT       NOT NULL,
    [attendance_status] VARCHAR (20) NOT NULL,
    CONSTRAINT [PK_group_attendance] PRIMARY KEY CLUSTERED ([group_id] ASC, [user_id] ASC),
    CONSTRAINT [FK_group_attendance_group_id] FOREIGN KEY ([group_id]) REFERENCES [dbo].[Event_Data] ([Event_Id]),
    CONSTRAINT [FK_group_attendance_user_id] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Mountains] (
    [Mountain_Id]                   BIGINT         IDENTITY (1, 1) NOT NULL,
    [Mountain_Name]                 NVARCHAR (100) NOT NULL,
    [Location]                      NVARCHAR (50)  NOT NULL,
    [Altitude]                      INT            NOT NULL,
    [Difficulty_Level]              INT            NOT NULL,
    [Mountains_Permit_Required]     BIT            NULL,
    [National_Park_Permit_Required] BIT            NULL,
    [Longitude]                     DECIMAL (9, 6) NULL,
    [Latitude]                      DECIMAL (9, 6) NULL,
    CONSTRAINT [PK_Mountains] PRIMARY KEY CLUSTERED ([Mountain_Id] ASC)
);


GO

CREATE TABLE [dbo].[ArticleLike] (
    [Like_ID]     INT      IDENTITY (1, 1) NOT NULL,
    [User_ID]     BIGINT   NOT NULL,
    [Article_ID]  INT      NOT NULL,
    [CreatedDate] DATETIME DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ArticleLike] PRIMARY KEY CLUSTERED ([Like_ID] ASC),
    CONSTRAINT [FK_ArticleLike_Article] FOREIGN KEY ([Article_ID]) REFERENCES [dbo].[Article] ([Article_ID]),
    CONSTRAINT [FK_ArticleLike_User] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [UQ_ArticleLike] UNIQUE NONCLUSTERED ([User_ID] ASC, [Article_ID] ASC)
);


GO

CREATE TABLE [dbo].[geometry_columns] (
    [f_table_catalog]   VARCHAR (128) NOT NULL,
    [f_table_schema]    VARCHAR (128) NOT NULL,
    [f_table_name]      VARCHAR (256) NOT NULL,
    [f_geometry_column] VARCHAR (256) NOT NULL,
    [coord_dimension]   INT           NOT NULL,
    [srid]              INT           NOT NULL,
    [geometry_type]     VARCHAR (30)  NOT NULL,
    CONSTRAINT [geometry_columns_pk] PRIMARY KEY CLUSTERED ([f_table_catalog] ASC, [f_table_schema] ASC, [f_table_name] ASC, [f_geometry_column] ASC)
);


GO

CREATE TABLE [dbo].[BackgroundJobRuns] (
    [Id]             BIGINT          IDENTITY (1, 1) NOT NULL,
    [JobType]        VARCHAR (50)    NOT NULL,
    [Status]         VARCHAR (20)    NOT NULL,
    [HangfireJobId]  VARCHAR (100)   NULL,
    [DistanceMeters] DECIMAL (12, 2) NOT NULL,
    [CreatedAt]      DATETIME2 (0)   CONSTRAINT [DF_BackgroundJobRuns_CreatedAt] DEFAULT (sysutcdatetime()) NOT NULL,
    [StartedAt]      DATETIME2 (0)   NULL,
    [FinishedAt]     DATETIME2 (0)   NULL,
    [ErrorMessage]   NVARCHAR (500)  NULL,
    CONSTRAINT [PK_BackgroundJobRuns] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CK_BackgroundJobRuns_DistanceMeters] CHECK ([DistanceMeters]>=(0.01) AND [DistanceMeters]<=(10000.00)),
    CONSTRAINT [CK_BackgroundJobRuns_Status] CHECK ([Status]='Failed' OR [Status]='Succeeded' OR [Status]='RetryPending' OR [Status]='Processing' OR [Status]='Queued' OR [Status]='Pending')
);


GO

CREATE TABLE [dbo].[user_skill_tags] (
    [user_id]      BIGINT       NOT NULL,
    [tag_id]       BIGINT       NOT NULL,
    [source]       VARCHAR (20) NOT NULL,
    [is_displayed] BIT          CONSTRAINT [DF_user_skill_tags_is_displayed] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_user_skill_tags] PRIMARY KEY CLUSTERED ([user_id] ASC, [tag_id] ASC),
    CONSTRAINT [FK_user_skill_tags_tag_id] FOREIGN KEY ([tag_id]) REFERENCES [dbo].[skill_tags] ([tag_id]),
    CONSTRAINT [FK_user_skill_tags_user_id] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Event_Data] (
    [Event_Id]          BIGINT         IDENTITY (1, 1) NOT NULL,
    [Mountain_Id]       BIGINT         NOT NULL,
    [Event_Name]        NVARCHAR (50)  NOT NULL,
    [Maximum_Number]    INT            NOT NULL,
    [Activity_Status]   NVARCHAR (10)  NOT NULL,
    [Activity_Photo]    VARCHAR (255)  NULL,
    [Description]       NVARCHAR (MAX) NULL,
    [Event_Date]        DATETIME       NOT NULL,
    [Review_Required]   BIT            NULL,
    [Review_Status]     NVARCHAR (10)  NULL,
    [Has_Active_Report] BIT            NULL,
    [Leader_User_Id]    BIGINT         NOT NULL,
    [Event_Start_Time]  DATETIME       NULL,
    [Event_End_Time]    DATETIME       NULL,
    CONSTRAINT [PK_Event_Data] PRIMARY KEY CLUSTERED ([Event_Id] ASC),
    CONSTRAINT [FK_Event_Data_Mountain_Id] FOREIGN KEY ([Mountain_Id]) REFERENCES [dbo].[Mountains] ([Mountain_Id])
);


GO

CREATE TABLE [dbo].[MountainEquipmentSuggestions] (
    [SuggestionId]      BIGINT         IDENTITY (1, 1) NOT NULL,
    [MountainId]        BIGINT         NOT NULL,
    [EquipmentId]       BIGINT         NOT NULL,
    [Season]            NVARCHAR (20)  NOT NULL,
    [MinimumDays]       INT            DEFAULT ((1)) NOT NULL,
    [MaximumDays]       INT            NULL,
    [IntensityLevel]    NVARCHAR (20)  NULL,
    [ExperienceLevel]   NVARCHAR (20)  NULL,
    [SuggestedQuantity] INT            DEFAULT ((1)) NOT NULL,
    [RequirementLevel]  NVARCHAR (20)  NOT NULL,
    [Notes]             NVARCHAR (300) NULL,
    CONSTRAINT [PK_MountainEquipmentSuggestions] PRIMARY KEY CLUSTERED ([SuggestionId] ASC),
    CONSTRAINT [FK_MountainEquipmentSuggestions_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [dbo].[Equipments] ([EquipmentId]),
    CONSTRAINT [FK_MountainEquipmentSuggestions_MountainId] FOREIGN KEY ([MountainId]) REFERENCES [dbo].[Mountains] ([Mountain_Id])
);


GO

CREATE TABLE [dbo].[skill_tags] (
    [tag_id]           BIGINT       IDENTITY (1, 1) NOT NULL,
    [category]         VARCHAR (20) NOT NULL,
    [tag_name]         VARCHAR (50) NOT NULL,
    [parent_tag_id]    BIGINT       NULL,
    [unlock_condition] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_skill_tags] PRIMARY KEY CLUSTERED ([tag_id] ASC),
    CONSTRAINT [FK_skill_tags_parent_tag_id] FOREIGN KEY ([parent_tag_id]) REFERENCES [dbo].[skill_tags] ([tag_id])
);


GO

CREATE TABLE [dbo].[AlertSegments] (
    [AlertSegmentId]  BIGINT            IDENTITY (1, 1) NOT NULL,
    [AlertId]         BIGINT            NOT NULL,
    [SegmentName]     NVARCHAR (200)    NULL,
    [Shape]           [sys].[geography] NOT NULL,
    [SourceFeatureId] NVARCHAR (100)    NULL,
    [SegmentLevel]    TINYINT           NULL,
    [Description]     NVARCHAR (1000)   NULL,
    CONSTRAINT [PK_AlertSegments] PRIMARY KEY CLUSTERED ([AlertSegmentId] ASC),
    CONSTRAINT [CK_AlertSegments_SegmentLevel] CHECK ([SegmentLevel] IS NULL OR [SegmentLevel]>=(1) AND [SegmentLevel]<=(5)),
    CONSTRAINT [CK_AlertSegments_SRID] CHECK ([Shape].[STSrid]=(4326)),
    CONSTRAINT [FK_AlertSegments_DisasterAlerts] FOREIGN KEY ([AlertId]) REFERENCES [dbo].[DisasterAlerts] ([AlertId])
);


GO

CREATE TABLE [dbo].[Favorite] (
    [Favorite_ID] INT      IDENTITY (1, 1) NOT NULL,
    [User_ID]     BIGINT   NOT NULL,
    [Article_ID]  INT      NOT NULL,
    [CreatedDate] DATETIME DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Favorite] PRIMARY KEY CLUSTERED ([Favorite_ID] ASC),
    CONSTRAINT [FK_Favorite_Article] FOREIGN KEY ([Article_ID]) REFERENCES [dbo].[Article] ([Article_ID]),
    CONSTRAINT [FK_Favorite_User] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [UQ_Favorite] UNIQUE NONCLUSTERED ([User_ID] ASC, [Article_ID] ASC)
);


GO

CREATE TABLE [dbo].[TrailFeatures] (
    [FeatureId]          BIGINT            IDENTITY (1, 1) NOT NULL,
    [Trail_Id]           BIGINT            NOT NULL,
    [FeatureType]        VARCHAR (20)      NOT NULL,
    [FeatureName]        NVARCHAR (120)    NOT NULL,
    [Location]           [sys].[geography] NOT NULL,
    [FeatureDescription] NVARCHAR (1000)   NULL,
    [ReliabilityLevel]   TINYINT           NOT NULL,
    [IsAvailable]        BIT               NOT NULL,
    [DataSource]         NVARCHAR (200)    NULL,
    CONSTRAINT [PK_TrailFeatures] PRIMARY KEY CLUSTERED ([FeatureId] ASC),
    CONSTRAINT [FK_TrailFeatures_Trail_Id] FOREIGN KEY ([Trail_Id]) REFERENCES [dbo].[Trails] ([Trail_Id])
);


GO

CREATE TABLE [dbo].[RefreshTokens] (
    [RefreshTokenId]  BIGINT        IDENTITY (1, 1) NOT NULL,
    [UserId]          BIGINT        NOT NULL,
    [Token]           VARCHAR (255) NOT NULL,
    [ExpiresAt]       DATETIME2 (0) NOT NULL,
    [CreatedAt]       DATETIME2 (0) NOT NULL,
    [RevokedAt]       DATETIME2 (0) NULL,
    [ReplacedByToken] VARCHAR (255) NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([RefreshTokenId] ASC),
    CONSTRAINT [FK_RefreshTokens_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[TripReports] (
    [ReportId]         BIGINT            IDENTITY (1, 1) NOT NULL,
    [TripId]           BIGINT            NULL,
    [Trail_Id]         BIGINT            NOT NULL,
    [ReporterUserId]   BIGINT            NULL,
    [SourceType]       VARCHAR (20)      NOT NULL,
    [ReportType]       VARCHAR (30)      NOT NULL,
    [ReportContent]    NVARCHAR (3000)   NOT NULL,
    [Location]         [sys].[geography] NULL,
    [OccurredAt]       DATETIME2 (0)     NULL,
    [ReviewStatus]     VARCHAR (20)      NOT NULL,
    [ReviewedByUserId] BIGINT            NULL,
    [ReviewedAt]       DATETIME2 (0)     NULL,
    [RewardPoints]     INT               NOT NULL,
    CONSTRAINT [PK_TripReports] PRIMARY KEY CLUSTERED ([ReportId] ASC),
    CONSTRAINT [FK_TripReports_ReporterUserId] FOREIGN KEY ([ReporterUserId]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [FK_TripReports_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [FK_TripReports_TrId] FOREIGN KEY ([Trail_Id]) REFERENCES [dbo].[Trails] ([Trail_Id])
);


GO

CREATE TABLE [dbo].[levels] (
    [level_id]   BIGINT       IDENTITY (1, 1) NOT NULL,
    [level_name] VARCHAR (50) NOT NULL,
    [min_xp]     INT          NOT NULL,
    [max_xp]     INT          NOT NULL,
    CONSTRAINT [PK_levels] PRIMARY KEY CLUSTERED ([level_id] ASC)
);


GO

CREATE TABLE [dbo].[Announcement] (
    [Announcement_ID] INT            IDENTITY (1, 1) NOT NULL,
    [Title]           NVARCHAR (100) NOT NULL,
    [Content]         NVARCHAR (MAX) NOT NULL,
    [CreatedDate]     DATETIME       DEFAULT (getdate()) NOT NULL,
    [UpdateDate]      DATETIME       NULL,
    [Status]          TINYINT        DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([Announcement_ID] ASC)
);


GO

CREATE TABLE [dbo].[CommentImage] (
    [Image_ID]    INT            IDENTITY (1, 1) NOT NULL,
    [Comment_ID]  INT            NOT NULL,
    [ImagePath]   NVARCHAR (500) NOT NULL,
    [CreatedDate] DATETIME       DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_CommentImage] PRIMARY KEY CLUSTERED ([Image_ID] ASC),
    CONSTRAINT [FK_CommentImage_Comment] FOREIGN KEY ([Comment_ID]) REFERENCES [dbo].[Comment] ([Comment_ID])
);


GO

CREATE TABLE [dbo].[ArticleView] (
    [View_ID]    BIGINT   IDENTITY (1, 1) NOT NULL,
    [Article_ID] INT      NOT NULL,
    [User_ID]    BIGINT   NULL,
    [ViewedDate] DATETIME CONSTRAINT [DF_ArticleView_ViewedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ArticleView] PRIMARY KEY CLUSTERED ([View_ID] ASC),
    CONSTRAINT [FK_ArticleView_Article] FOREIGN KEY ([Article_ID]) REFERENCES [dbo].[Article] ([Article_ID]),
    CONSTRAINT [FK_ArticleView_User] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[TrailSegments] (
    [TrailSegment_Id] BIGINT            IDENTITY (1, 1) NOT NULL,
    [Trail_Id]        BIGINT            NOT NULL,
    [Shape]           [sys].[geography] NOT NULL,
    [Source]          NVARCHAR (50)     NULL,
    [Source_Id]       NVARCHAR (100)    NULL,
    [Source_Url]      NVARCHAR (500)    NULL,
    CONSTRAINT [PK_TrailSegments] PRIMARY KEY CLUSTERED ([TrailSegment_Id] ASC),
    CONSTRAINT [CK_TrailSegments_GeometryType] CHECK ([Shape].[STGeometryType]()='MultiLineString' OR [Shape].[STGeometryType]()='LineString'),
    CONSTRAINT [CK_TrailSegments_SRID] CHECK ([Shape].[STSrid]=(4326)),
    CONSTRAINT [FK_TrailSegments_Trails] FOREIGN KEY ([Trail_Id]) REFERENCES [dbo].[Trails] ([Trail_Id])
);


GO

CREATE TABLE [dbo].[IndicatorSegments] (
    [IndicatorSegmentId] BIGINT            IDENTITY (1, 1) NOT NULL,
    [IndicatorId]        BIGINT            NOT NULL,
    [SegmentName]        NVARCHAR (200)    NULL,
    [Shape]              [sys].[geography] NOT NULL,
    [SourceFeatureId]    NVARCHAR (100)    NULL,
    [SegmentLevel]       TINYINT           NULL,
    [Description]        NVARCHAR (1000)   NULL,
    CONSTRAINT [PK_IndicatorSegments] PRIMARY KEY CLUSTERED ([IndicatorSegmentId] ASC),
    CONSTRAINT [CK_IndicatorSegments_SegmentLevel] CHECK ([SegmentLevel] IS NULL OR [SegmentLevel]>=(1) AND [SegmentLevel]<=(5)),
    CONSTRAINT [CK_IndicatorSegments_SRID] CHECK ([Shape].[STSrid]=(4326)),
    CONSTRAINT [FK_IndicatorSegments_Indicators] FOREIGN KEY ([IndicatorId]) REFERENCES [dbo].[Indicators] ([IndicatorId]) ON DELETE CASCADE
);


GO

CREATE TABLE [dbo].[EventLeader_Rating] (
    [Review_Id]     BIGINT         IDENTITY (1, 1) NOT NULL,
    [Event_Id]      BIGINT         NOT NULL,
    [User_Id]       BIGINT         NOT NULL,
    [Leader_Rating] INT            NOT NULL,
    [Comment]       NVARCHAR (500) NOT NULL,
    [Created_At]    DATETIME       NOT NULL,
    CONSTRAINT [PK_EventLeader_Rating] PRIMARY KEY CLUSTERED ([Review_Id] ASC),
    CONSTRAINT [FK_EventLeader_Rating_Event_Id] FOREIGN KEY ([Event_Id]) REFERENCES [dbo].[Event_Data] ([Event_Id]),
    CONSTRAINT [FK_EventLeader_Rating_User_Id] FOREIGN KEY ([User_Id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Notify] (
    [Notification_Id]  BIGINT         IDENTITY (1, 1) NOT NULL,
    [User_Id]          BIGINT         NOT NULL,
    [Type]             VARCHAR (30)   NOT NULL,
    [Title]            NVARCHAR (100) NOT NULL,
    [Content]          NVARCHAR (500) NOT NULL,
    [Related_FormType] VARCHAR (30)   NOT NULL,
    [Related_Id]       BIGINT         NOT NULL,
    [UsingPipeline]    VARCHAR (20)   NOT NULL,
    [Is_Read]          BIT            NOT NULL,
    [Created_At]       DATETIME       NOT NULL,
    CONSTRAINT [PK_Notify] PRIMARY KEY CLUSTERED ([Notification_Id] ASC),
    CONSTRAINT [FK_Notify_User_Id] FOREIGN KEY ([User_Id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Equipments] (
    [EquipmentId]        BIGINT         IDENTITY (1, 1) NOT NULL,
    [CategoryId]         BIGINT         NOT NULL,
    [EquipmentName]      NVARCHAR (100) NOT NULL,
    [StandardWeightGram] INT            NOT NULL,
    [RequirementLevel]   NVARCHAR (20)  NOT NULL,
    [Description]        NVARCHAR (500) NULL,
    [ImageUrl]           NVARCHAR (500) NULL,
    [IsActive]           BIT            DEFAULT ((1)) NOT NULL,
    [UpdatedAt]          DATETIME2 (7)  NULL,
    CONSTRAINT [PK_Equipments] PRIMARY KEY CLUSTERED ([EquipmentId] ASC),
    CONSTRAINT [CK_Equipments_StandardWeightGram] CHECK ([StandardWeightGram]>=(0)),
    CONSTRAINT [FK_Equipments_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[EquipmentCategories] ([CategoryId])
);


GO

CREATE TABLE [dbo].[ArticleImage] (
    [Image_ID]    INT            IDENTITY (1, 1) NOT NULL,
    [Article_ID]  INT            NOT NULL,
    [ImagePath]   NVARCHAR (500) NOT NULL,
    [SortOrder]   INT            DEFAULT ((1)) NOT NULL,
    [CreatedDate] DATETIME       DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ArticleImage] PRIMARY KEY CLUSTERED ([Image_ID] ASC),
    CONSTRAINT [FK_ArticleImage_Article] FOREIGN KEY ([Article_ID]) REFERENCES [dbo].[Article] ([Article_ID])
);


GO

CREATE TABLE [dbo].[Indicators] (
    [IndicatorId]          BIGINT          IDENTITY (1, 1) NOT NULL,
    [IndicatorName]        NVARCHAR (120)  NOT NULL,
    [IndicatorType]        VARCHAR (30)    NOT NULL,
    [Weight]               DECIMAL (6, 3)  NOT NULL,
    [IndicatorLevel]       TINYINT         NULL,
    [IndicatorDescription] NVARCHAR (1500) NULL,
    [DataSource]           NVARCHAR (500)  NULL,
    [IsActive]             BIT             CONSTRAINT [DF_Indicators_IsActive] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Indicators] PRIMARY KEY CLUSTERED ([IndicatorId] ASC),
    CONSTRAINT [CK_Indicators_IndicatorLevel] CHECK ([IndicatorLevel] IS NULL OR [IndicatorLevel]>=(1) AND [IndicatorLevel]<=(5)),
    CONSTRAINT [CK_Indicators_Weight] CHECK ([Weight]>=(0))
);


GO

CREATE TABLE [dbo].[Suspension_Schedule] (
    [Ban_Id]                     BIGINT         IDENTITY (1, 1) NOT NULL,
    [User_Id]                    BIGINT         NOT NULL,
    [Event_Id]                   BIGINT         NULL,
    [Reason]                     NVARCHAR (200) NOT NULL,
    [Suspension_Status]          NVARCHAR (10)  NOT NULL,
    [Suspension_Expiration_Time] DATETIME       NOT NULL,
    [Created_At]                 DATETIME       NOT NULL,
    CONSTRAINT [PK_Suspension_Schedule] PRIMARY KEY CLUSTERED ([Ban_Id] ASC),
    CONSTRAINT [FK_Suspension_Schedule_Event_Id] FOREIGN KEY ([Event_Id]) REFERENCES [dbo].[Event_Data] ([Event_Id]),
    CONSTRAINT [FK_Suspension_Schedule_User_Id] FOREIGN KEY ([User_Id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Report] (
    [Report_ID]   INT            IDENTITY (1, 1) NOT NULL,
    [User_ID]     BIGINT         NOT NULL,
    [Article_ID]  INT            NOT NULL,
    [Reason]      NVARCHAR (300) NOT NULL,
    [Reply]       NVARCHAR (300) NULL,
    [Admin_ID]    BIGINT         NULL,
    [ReviewDate]  DATETIME       NULL,
    [Status]      TINYINT        DEFAULT ((0)) NOT NULL,
    [CreatedDate] DATETIME       DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Report] PRIMARY KEY CLUSTERED ([Report_ID] ASC),
    CONSTRAINT [FK_Report_Admin] FOREIGN KEY ([Admin_ID]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [FK_Report_Article] FOREIGN KEY ([Article_ID]) REFERENCES [dbo].[Article] ([Article_ID]),
    CONSTRAINT [FK_Report_User] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[users] (
    [user_id]                  BIGINT        IDENTITY (1, 1) NOT NULL,
    [role]                     VARCHAR (20)  NOT NULL,
    [nickname]                 VARCHAR (20)  NOT NULL,
    [email]                    VARCHAR (255) NOT NULL,
    [password_hash]            VARCHAR (255) NOT NULL,
    [account_status]           VARCHAR (20)  NULL,
    [avatar_url]               VARCHAR (255) NULL,
    [avatar_blur_state]        VARCHAR (20)  NULL,
    [bio]                      VARCHAR (500) NULL,
    [current_level_id]         BIGINT        NOT NULL,
    [total_xp]                 INT           NOT NULL,
    [region_preference]        VARCHAR (100) NULL,
    [difficulty_preference]    VARCHAR (50)  NULL,
    [created_at]               DATETIME      NOT NULL,
    [last_active_at]           DATETIME      NOT NULL,
    [displayed_achievement_id] BIGINT        NULL,
    CONSTRAINT [PK_users] PRIMARY KEY CLUSTERED ([user_id] ASC),
    CONSTRAINT [FK_users_current_level_id] FOREIGN KEY ([current_level_id]) REFERENCES [dbo].[levels] ([level_id]),
    CONSTRAINT [FK_users_displayed_achievement_id] FOREIGN KEY ([displayed_achievement_id]) REFERENCES [dbo].[achievements] ([achievement_id]) ON DELETE SET NULL,
    CONSTRAINT [UQ__users__AB6E6164F6B68D47] UNIQUE NONCLUSTERED ([email] ASC)
);


GO

CREATE TABLE [dbo].[spatial_ref_sys] (
    [srid]      INT            NOT NULL,
    [auth_name] VARCHAR (256)  NULL,
    [auth_srid] INT            NULL,
    [srtext]    VARCHAR (2048) NULL,
    [proj4text] VARCHAR (2048) NULL,
    PRIMARY KEY CLUSTERED ([srid] ASC)
);


GO

CREATE TABLE [dbo].[TrailIndicators] (
    [Trail_Id]                BIGINT          NOT NULL,
    [IndicatorId]             BIGINT          NOT NULL,
    [OverlapRatio]            DECIMAL (7, 6)  NULL,
    [DistanceMeters]          DECIMAL (12, 2) NULL,
    [IndicatorWeightSnapshot] DECIMAL (6, 3)  NOT NULL,
    [RawScore]                DECIMAL (10, 4) NULL,
    [EvaluatedScore]          DECIMAL (10, 4) NULL,
    [EvaluatedAt]             DATETIME2 (0)   CONSTRAINT [DF_TrailIndicators_EvaluatedAt] DEFAULT (sysdatetime()) NOT NULL,
    CONSTRAINT [PK_TrailIndicators] PRIMARY KEY CLUSTERED ([Trail_Id] ASC, [IndicatorId] ASC),
    CONSTRAINT [CK_TrailIndicators_DistanceMeters] CHECK ([DistanceMeters] IS NULL OR [DistanceMeters]>=(0)),
    CONSTRAINT [CK_TrailIndicators_IndicatorWeightSnapshot] CHECK ([IndicatorWeightSnapshot]>=(0)),
    CONSTRAINT [CK_TrailIndicators_OverlapRatio] CHECK ([OverlapRatio] IS NULL OR [OverlapRatio]>=(0) AND [OverlapRatio]<=(1)),
    CONSTRAINT [FK_TrailIndicators_Indicators] FOREIGN KEY ([IndicatorId]) REFERENCES [dbo].[Indicators] ([IndicatorId]),
    CONSTRAINT [FK_TrailIndicators_Trails] FOREIGN KEY ([Trail_Id]) REFERENCES [dbo].[Trails] ([Trail_Id]) ON DELETE CASCADE
);


GO

CREATE TABLE [dbo].[achievements] (
    [achievement_id]  BIGINT        IDENTITY (1, 1) NOT NULL,
    [name]            VARCHAR (100) NOT NULL,
    [description]     VARCHAR (255) NOT NULL,
    [rarity]          VARCHAR (20)  NOT NULL,
    [condition_type]  VARCHAR (50)  NOT NULL,
    [condition_value] VARCHAR (100) NOT NULL,
    CONSTRAINT [PK_achievements] PRIMARY KEY CLUSTERED ([achievement_id] ASC)
);


GO

CREATE TABLE [dbo].[hike_records] (
    [record_id]       BIGINT        IDENTITY (1, 1) NOT NULL,
    [user_id]         BIGINT        NOT NULL,
    [mountain_id]     BIGINT        NOT NULL,
    [hike_date]       DATE          NOT NULL,
    [companion_count] INT           NOT NULL,
    [note]            VARCHAR (500) NOT NULL,
    [verified]        BIT           NOT NULL,
    CONSTRAINT [PK_hike_records] PRIMARY KEY CLUSTERED ([record_id] ASC),
    CONSTRAINT [FK_hike_records_mountain_id] FOREIGN KEY ([mountain_id]) REFERENCES [dbo].[Mountains] ([Mountain_Id]),
    CONSTRAINT [FK_hike_records_user_id] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Comment] (
    [Comment_ID]       INT             IDENTITY (1, 1) NOT NULL,
    [Article_ID]       INT             NOT NULL,
    [User_ID]          BIGINT          NOT NULL,
    [Content]          NVARCHAR (1000) NOT NULL,
    [ParentComment_ID] INT             NULL,
    [ReplyToUser_ID]   BIGINT          NULL,
    [CreatedDate]      DATETIME        DEFAULT (getdate()) NOT NULL,
    [UpdateDate]       DATETIME        NULL,
    [Status]           TINYINT         DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Comment] PRIMARY KEY CLUSTERED ([Comment_ID] ASC),
    CONSTRAINT [FK_Comment_Article] FOREIGN KEY ([Article_ID]) REFERENCES [dbo].[Article] ([Article_ID]),
    CONSTRAINT [FK_Comment_Parent] FOREIGN KEY ([ParentComment_ID]) REFERENCES [dbo].[Comment] ([Comment_ID]),
    CONSTRAINT [FK_Comment_ReplyUser] FOREIGN KEY ([ReplyToUser_ID]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [FK_Comment_User] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[ArticleTag] (
    [Article_ID] INT NOT NULL,
    [Tag_ID]     INT NOT NULL,
    CONSTRAINT [PK_ArticleTag] PRIMARY KEY CLUSTERED ([Article_ID] ASC, [Tag_ID] ASC),
    CONSTRAINT [FK_ArticleTag_Article] FOREIGN KEY ([Article_ID]) REFERENCES [dbo].[Article] ([Article_ID]),
    CONSTRAINT [FK_ArticleTag_Tag] FOREIGN KEY ([Tag_ID]) REFERENCES [dbo].[Tag] ([Tag_ID])
);


GO

CREATE TABLE [dbo].[PersonalEquipmentDetails] (
    [DetailId]            BIGINT         IDENTITY (1, 1) NOT NULL,
    [ListId]              BIGINT         NOT NULL,
    [EquipmentId]         BIGINT         NULL,
    [CustomEquipmentName] NVARCHAR (100) NULL,
    [Quantity]            INT            NOT NULL,
    [UnitWeightGram]      INT            NOT NULL,
    [TotalWeightGram]     INT            NOT NULL,
    [RequirementLevel]    NVARCHAR (20)  NULL,
    [IsPrepared]          BIT            DEFAULT ((0)) NOT NULL,
    [SortOrder]           INT            DEFAULT ((0)) NOT NULL,
    [Notes]               NVARCHAR (300) NULL,
    CONSTRAINT [PK_PersonalEquipmentDetails] PRIMARY KEY CLUSTERED ([DetailId] ASC),
    CONSTRAINT [CK_PersonalEquipmentDetails_Quantity] CHECK ([Quantity]>(0)),
    CONSTRAINT [CK_PersonalEquipmentDetails_UnitWeightGram] CHECK ([UnitWeightGram]>=(0)),
    CONSTRAINT [FK_PersonalEquipmentDetails_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [dbo].[Equipments] ([EquipmentId]),
    CONSTRAINT [FK_PersonalEquipmentDetails_ListId] FOREIGN KEY ([ListId]) REFERENCES [dbo].[PersonalEquipmentLists] ([ListId])
);


GO

CREATE TABLE [dbo].[user_achievements] (
    [user_id]        BIGINT   NOT NULL,
    [achievement_id] BIGINT   NOT NULL,
    [unlocked_at]    DATETIME NOT NULL,
    CONSTRAINT [PK_user_achievements] PRIMARY KEY CLUSTERED ([user_id] ASC, [achievement_id] ASC),
    CONSTRAINT [FK_user_achievements_achievement_id] FOREIGN KEY ([achievement_id]) REFERENCES [dbo].[achievements] ([achievement_id]),
    CONSTRAINT [FK_user_achievements_user_id] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[Event_Report_Complaint] (
    [Report_Event_Id] BIGINT         IDENTITY (1, 1) NOT NULL,
    [User_Id]         BIGINT         NOT NULL,
    [Event_Id]        BIGINT         NOT NULL,
    [Report_Reason]   NVARCHAR (500) NOT NULL,
    [Evidence_Url]    VARCHAR (255)  NOT NULL,
    [Report_Status]   NVARCHAR (10)  NOT NULL,
    [Created_At]      DATETIME       NOT NULL,
    [Report_Title]    NVARCHAR (100) NULL,
    CONSTRAINT [PK_Event_Report_Complaint] PRIMARY KEY CLUSTERED ([Report_Event_Id] ASC),
    CONSTRAINT [FK_Event_Report_Complaint_Event_Id] FOREIGN KEY ([Event_Id]) REFERENCES [dbo].[Event_Data] ([Event_Id]),
    CONSTRAINT [FK_Event_Report_Complaint_User_Id] FOREIGN KEY ([User_Id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE TABLE [dbo].[AlertsTrails] (
    [AlertTrail_Id]      BIGINT          IDENTITY (1, 1) NOT NULL,
    [AlertId]            BIGINT          NOT NULL,
    [Trail_Id]           BIGINT          NOT NULL,
    [Reason_Description] NVARCHAR (2000) NULL,
    CONSTRAINT [PK_AlertsTrails] PRIMARY KEY CLUSTERED ([AlertTrail_Id] ASC),
    CONSTRAINT [FK_AlertsTrails_Alert_Id] FOREIGN KEY ([AlertId]) REFERENCES [dbo].[DisasterAlerts] ([AlertId]),
    CONSTRAINT [FK_AlertsTrails_Trail_Id] FOREIGN KEY ([Trail_Id]) REFERENCES [dbo].[Trails] ([Trail_Id]),
    CONSTRAINT [UQ_AlertsTrails_AlertId_TrailId] UNIQUE NONCLUSTERED ([AlertId] ASC, [Trail_Id] ASC)
);


GO

CREATE TABLE [dbo].[EquipmentCategories] (
    [CategoryId]   BIGINT        IDENTITY (1, 1) NOT NULL,
    [CategoryName] NVARCHAR (50) NOT NULL,
    [SortOrder]    INT           DEFAULT ((0)) NOT NULL,
    [IsActive]     BIT           DEFAULT ((1)) NOT NULL,
    [CreatedAt]    DATETIME2 (7) NOT NULL,
    CONSTRAINT [PK_EquipmentCategories] PRIMARY KEY CLUSTERED ([CategoryId] ASC)
);


GO

CREATE NONCLUSTERED INDEX [IX_BackgroundJobRuns_JobType_Status_CreatedAt]
    ON [dbo].[BackgroundJobRuns]([JobType] ASC, [Status] ASC, [CreatedAt] ASC);


GO
