CREATE TABLE [dbo].[Trails] (
[Trail_Id] bigint NOT NULL IDENTITY(1,1),
[Trail_Name] nvarchar(120) NOT NULL,
[Region] nvarchar(80) NOT NULL,
[Difficulty_Level] int NOT NULL,
[Distance_Km] decimal(7,2),
[EstimatedHours] decimal(6,2),
[Permit_Required] bit NOT NULL,
[Guide_Required] bit NOT NULL,
[RegulationNote] nvarchar(1000),
[IsPublished] bit NOT NULL,
PRIMARY KEY ([Trail_Id])
);

CREATE TABLE [dbo].[Notification] (
[Notification_ID] bigint NOT NULL IDENTITY(1,1),
[User_ID] bigint NOT NULL,
[Sender_User_ID] bigint NOT NULL,
[Article_ID] int,
[Comment_ID] int,
[Type] tinyint NOT NULL,
[Message] nvarchar(300) NOT NULL,
[IsRead] bit DEFAULT ((0)) NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
PRIMARY KEY ([Notification_ID])
);

CREATE TABLE [dbo].[group_attendance] (
[group_id] bigint NOT NULL,
[user_id] bigint NOT NULL,
[attendance_status] varchar(20) NOT NULL,
PRIMARY KEY ([group_id], [user_id])
);

CREATE TABLE [dbo].[skill_tags] (
[tag_id] bigint NOT NULL IDENTITY(1,1),
[category] varchar(20) NOT NULL,
[tag_name] varchar(50) NOT NULL,
[parent_tag_id] bigint,
[unlock_condition] varchar(50) NOT NULL,
PRIMARY KEY ([tag_id])
);

CREATE TABLE [dbo].[RefreshTokens] (
[RefreshTokenId] bigint NOT NULL IDENTITY(1,1),
[UserId] bigint NOT NULL,
[Token] varchar(255) NOT NULL,
[ExpiresAt] datetime2(0) NOT NULL,
[CreatedAt] datetime2(0) NOT NULL,
[RevokedAt] datetime2(0),
[ReplacedByToken] varchar(255),
PRIMARY KEY ([RefreshTokenId])
);

CREATE TABLE [dbo].[Announcement] (
[Announcement_ID] int NOT NULL IDENTITY(1,1),
[Title] nvarchar(100) NOT NULL,
[Content] nvarchar(max) NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
[UpdateDate] datetime,
[Status] tinyint DEFAULT ((1)) NOT NULL,
PRIMARY KEY ([Announcement_ID])
);

CREATE TABLE [dbo].[Mountains] (
[Mountain_Id] bigint NOT NULL IDENTITY(1,1),
[Mountain_Name] nvarchar(100) NOT NULL,
[Location] nvarchar(50) NOT NULL,
[Altitude] int NOT NULL,
[Difficulty_Level] int NOT NULL,
[Mountains_Permit_Required] bit,
[National_Park_Permit_Required] bit,
[Longitude] decimal(9,6),
[Latitude] decimal(9,6),
PRIMARY KEY ([Mountain_Id])
);

CREATE TABLE [dbo].[Event_Data] (
[Event_Id] bigint NOT NULL IDENTITY(1,1),
[Mountain_Id] bigint NOT NULL,
[Event_Name] nvarchar(50) NOT NULL,
[Maximum_Number] int NOT NULL,
[Activity_Status] nvarchar(10) NOT NULL,
[Activity_Photo] varchar(255),
[Description] nvarchar(max),
[Event_Date] datetime NOT NULL,
[Review_Required] bit,
[Review_Status] nvarchar(10),
[Has_Active_Report] bit,
[Leader_User_Id] bigint NOT NULL,
[Event_Start_Time] datetime,
[Event_End_Time] datetime,
PRIMARY KEY ([Event_Id])
);

CREATE TABLE [dbo].[TrailFeatures] (
[FeatureId] bigint NOT NULL IDENTITY(1,1),
[Trail_Id] bigint NOT NULL,
[FeatureType] varchar(20) NOT NULL,
[FeatureName] nvarchar(120) NOT NULL,
[Location] sys.geography NOT NULL,
[FeatureDescription] nvarchar(1000),
[ReliabilityLevel] tinyint NOT NULL,
[IsAvailable] bit NOT NULL,
[DataSource] nvarchar(200),
PRIMARY KEY ([FeatureId])
);

CREATE TABLE [dbo].[Notify] (
[Notification_Id] bigint NOT NULL IDENTITY(1,1),
[User_Id] bigint NOT NULL,
[Type] varchar(30) NOT NULL,
[Title] nvarchar(100) NOT NULL,
[Content] nvarchar(500) NOT NULL,
[Related_FormType] varchar(30) NOT NULL,
[Related_Id] bigint NOT NULL,
[UsingPipeline] varchar(20) NOT NULL,
[Is_Read] bit NOT NULL,
[Created_At] datetime NOT NULL,
PRIMARY KEY ([Notification_Id])
);

CREATE TABLE [dbo].[CommentImage] (
[Image_ID] int NOT NULL IDENTITY(1,1),
[Comment_ID] int NOT NULL,
[ImagePath] nvarchar(500) NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
PRIMARY KEY ([Image_ID])
);

CREATE TABLE [dbo].[ArticleView] (
[View_ID] bigint NOT NULL IDENTITY(1,1),
[Article_ID] int NOT NULL,
[User_ID] bigint,
[ViewedDate] datetime DEFAULT (getdate()) NOT NULL,
PRIMARY KEY ([View_ID])
);

CREATE TABLE [dbo].[Favorite] (
[Favorite_ID] int NOT NULL IDENTITY(1,1),
[User_ID] bigint NOT NULL,
[Article_ID] int NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
PRIMARY KEY ([Favorite_ID])
);

CREATE TABLE [dbo].[MountainEquipmentSuggestions] (
[SuggestionId] bigint NOT NULL IDENTITY(1,1),
[MountainId] bigint NOT NULL,
[EquipmentId] bigint NOT NULL,
[Season] nvarchar(20) NOT NULL,
[MinimumDays] int DEFAULT ((1)) NOT NULL,
[MaximumDays] int,
[IntensityLevel] nvarchar(20),
[ExperienceLevel] nvarchar(20),
[SuggestedQuantity] int DEFAULT ((1)) NOT NULL,
[RequirementLevel] nvarchar(20) NOT NULL,
[Notes] nvarchar(300),
PRIMARY KEY ([SuggestionId])
);

CREATE TABLE [dbo].[ArticleImage] (
[Image_ID] int NOT NULL IDENTITY(1,1),
[Article_ID] int NOT NULL,
[ImagePath] nvarchar(500) NOT NULL,
[SortOrder] int DEFAULT ((1)) NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
PRIMARY KEY ([Image_ID])
);

CREATE TABLE [dbo].[user_skill_tags] (
[user_id] bigint NOT NULL,
[tag_id] bigint NOT NULL,
[source] varchar(20) NOT NULL,
[is_displayed] bit DEFAULT ((0)) NOT NULL,
PRIMARY KEY ([user_id], [tag_id])
);

CREATE TABLE [dbo].[geometry_columns] (
[f_table_catalog] varchar(128) NOT NULL,
[f_table_schema] varchar(128) NOT NULL,
[f_table_name] varchar(256) NOT NULL,
[f_geometry_column] varchar(256) NOT NULL,
[coord_dimension] int NOT NULL,
[srid] int NOT NULL,
[geometry_type] varchar(30) NOT NULL,
PRIMARY KEY ([f_table_catalog], [f_table_schema], [f_table_name], [f_geometry_column])
);

CREATE TABLE [dbo].[TripReports] (
[ReportId] bigint NOT NULL IDENTITY(1,1),
[TripId] bigint,
[Trail_Id] bigint NOT NULL,
[ReporterUserId] bigint,
[SourceType] varchar(20) NOT NULL,
[ReportType] varchar(30) NOT NULL,
[ReportContent] nvarchar(3000) NOT NULL,
[Location] sys.geography,
[OccurredAt] datetime2(0),
[ReviewStatus] varchar(20) NOT NULL,
[ReviewedByUserId] bigint,
[ReviewedAt] datetime2(0),
[RewardPoints] int NOT NULL,
PRIMARY KEY ([ReportId])
);

CREATE TABLE [dbo].[Equipments] (
[EquipmentId] bigint NOT NULL IDENTITY(1,1),
[CategoryId] bigint NOT NULL,
[EquipmentName] nvarchar(100) NOT NULL,
[StandardWeightGram] int NOT NULL,
[RequirementLevel] nvarchar(20) NOT NULL,
[Description] nvarchar(500),
[ImageUrl] nvarchar(500),
[IsActive] bit DEFAULT ((1)) NOT NULL,
[UpdatedAt] datetime2(7),
PRIMARY KEY ([EquipmentId])
);

CREATE TABLE [dbo].[levels] (
[level_id] bigint NOT NULL IDENTITY(1,1),
[level_name] varchar(50) NOT NULL,
[min_xp] int NOT NULL,
[max_xp] int NOT NULL,
PRIMARY KEY ([level_id])
);

CREATE TABLE [dbo].[Report] (
[Report_ID] int NOT NULL IDENTITY(1,1),
[User_ID] bigint NOT NULL,
[Article_ID] int NOT NULL,
[Reason] nvarchar(300) NOT NULL,
[Reply] nvarchar(300),
[Admin_ID] bigint,
[ReviewDate] datetime,
[Status] tinyint DEFAULT ((0)) NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
PRIMARY KEY ([Report_ID])
);

CREATE TABLE [dbo].[achievements] (
[achievement_id] bigint NOT NULL IDENTITY(1,1),
[name] varchar(100) NOT NULL,
[description] varchar(255) NOT NULL,
[rarity] varchar(20) NOT NULL,
[condition_type] varchar(50) NOT NULL,
[condition_value] varchar(100) NOT NULL,
PRIMARY KEY ([achievement_id])
);

CREATE TABLE [dbo].[EventLeader_Rating] (
[Review_Id] bigint NOT NULL IDENTITY(1,1),
[Event_Id] bigint NOT NULL,
[User_Id] bigint NOT NULL,
[Leader_Rating] int NOT NULL,
[Comment] nvarchar(500) NOT NULL,
[Created_At] datetime NOT NULL,
PRIMARY KEY ([Review_Id])
);

CREATE TABLE [dbo].[Suspension_Schedule] (
[Ban_Id] bigint NOT NULL IDENTITY(1,1),
[User_Id] bigint NOT NULL,
[Event_Id] bigint,
[Reason] nvarchar(200) NOT NULL,
[Suspension_Status] nvarchar(10) NOT NULL,
[Suspension_Expiration_Time] datetime NOT NULL,
[Created_At] datetime NOT NULL,
PRIMARY KEY ([Ban_Id])
);

CREATE TABLE [dbo].[PersonalEquipmentDetails] (
[DetailId] bigint NOT NULL IDENTITY(1,1),
[ListId] bigint NOT NULL,
[EquipmentId] bigint,
[CustomEquipmentName] nvarchar(100),
[Quantity] int NOT NULL,
[UnitWeightGram] int NOT NULL,
[TotalWeightGram] int NOT NULL,
[RequirementLevel] nvarchar(20),
[IsPrepared] bit DEFAULT ((0)) NOT NULL,
[SortOrder] int DEFAULT ((0)) NOT NULL,
[Notes] nvarchar(300),
PRIMARY KEY ([DetailId])
);

CREATE TABLE [dbo].[TrailSegments] (
[TrailSegment_Id] bigint NOT NULL IDENTITY(1,1),
[Trail_Id] bigint NOT NULL,
[Shape] sys.geography NOT NULL,
[Source] nvarchar(50),
[Source_Id] nvarchar(100),
[Source_Url] nvarchar(500),
PRIMARY KEY ([TrailSegment_Id])
);

CREATE TABLE [dbo].[IndicatorSegments] (
[IndicatorSegmentId] bigint NOT NULL IDENTITY(1,1),
[IndicatorId] bigint NOT NULL,
[SegmentName] nvarchar(200),
[Shape] sys.geography NOT NULL,
[SourceFeatureId] nvarchar(100),
[SegmentLevel] tinyint,
[Description] nvarchar(1000),
PRIMARY KEY ([IndicatorSegmentId])
);

CREATE TABLE [dbo].[AlertsTrails] (
[AlertTrail_Id] bigint NOT NULL IDENTITY(1,1),
[AlertId] bigint NOT NULL,
[Trail_Id] bigint NOT NULL,
[Reason_Description] nvarchar(2000),
PRIMARY KEY ([AlertTrail_Id])
);

CREATE TABLE [dbo].[Comment] (
[Comment_ID] int NOT NULL IDENTITY(1,1),
[Article_ID] int NOT NULL,
[User_ID] bigint NOT NULL,
[Content] nvarchar(1000) NOT NULL,
[ParentComment_ID] int,
[ReplyToUser_ID] bigint,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
[UpdateDate] datetime,
[Status] tinyint DEFAULT ((1)) NOT NULL,
PRIMARY KEY ([Comment_ID])
);

CREATE TABLE [dbo].[ArticleTag] (
[Article_ID] int NOT NULL,
[Tag_ID] int NOT NULL,
PRIMARY KEY ([Article_ID], [Tag_ID])
);

CREATE TABLE [dbo].[TrailIndicators] (
[Trail_Id] bigint NOT NULL,
[IndicatorId] bigint NOT NULL,
[OverlapRatio] decimal(7,6),
[DistanceMeters] decimal(12,2),
[IndicatorWeightSnapshot] decimal(6,3) NOT NULL,
[RawScore] decimal(10,4),
[EvaluatedScore] decimal(10,4) NOT NULL,
[EvaluatedAt] datetime2(0) DEFAULT (sysdatetime()) NOT NULL,
PRIMARY KEY ([Trail_Id], [IndicatorId])
);

CREATE TABLE [dbo].[Indicators] (
[IndicatorId] bigint NOT NULL IDENTITY(1,1),
[IndicatorName] nvarchar(120) NOT NULL,
[IndicatorType] varchar(30) NOT NULL,
[Weight] decimal(6,3) NOT NULL,
[IndicatorLevel] tinyint,
[IndicatorDescription] nvarchar(1500),
[DataSource] nvarchar(500),
[IsActive] bit DEFAULT ((1)) NOT NULL,
PRIMARY KEY ([IndicatorId])
);

CREATE TABLE [dbo].[Event_Report_Complaint] (
[Report_Event_Id] bigint NOT NULL IDENTITY(1,1),
[User_Id] bigint NOT NULL,
[Event_Id] bigint NOT NULL,
[Report_Reason] nvarchar(500) NOT NULL,
[Evidence_Url] varchar(255) NOT NULL,
[Report_Status] nvarchar(10) NOT NULL,
[Created_At] datetime NOT NULL,
[Report_Title] nvarchar(100),
PRIMARY KEY ([Report_Event_Id])
);

CREATE TABLE [dbo].[EquipmentCategories] (
[CategoryId] bigint NOT NULL IDENTITY(1,1),
[CategoryName] nvarchar(50) NOT NULL,
[SortOrder] int DEFAULT ((0)) NOT NULL,
[IsActive] bit DEFAULT ((1)) NOT NULL,
[CreatedAt] datetime2(7) NOT NULL,
PRIMARY KEY ([CategoryId])
);

CREATE TABLE [dbo].[Article] (
[Article_ID] int NOT NULL IDENTITY(1,1),
[User_ID] bigint NOT NULL,
[Category_ID] int NOT NULL,
[Title] nvarchar(100) NOT NULL,
[Content] nvarchar(max) NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
[UpdateDate] datetime,
[Status] tinyint DEFAULT ((1)) NOT NULL,
PRIMARY KEY ([Article_ID])
);

CREATE TABLE [dbo].[hike_records] (
[record_id] bigint NOT NULL IDENTITY(1,1),
[user_id] bigint NOT NULL,
[mountain_id] bigint NOT NULL,
[hike_date] date NOT NULL,
[companion_count] int NOT NULL,
[note] varchar(500) NOT NULL,
[verified] bit NOT NULL,
PRIMARY KEY ([record_id])
);

CREATE TABLE [dbo].[ReviewApplications] (
[ApplicationId] bigint NOT NULL IDENTITY(1,1),
[ApplicantUserId] bigint NOT NULL,
[ApplicationType] varchar(20) NOT NULL,
[Related_Hike_DRecord_Id] bigint,
[RelatedReportId] bigint,
[Purpose] nvarchar(1000) NOT NULL,
[RequestPayloadJson] nvarchar(max),
[RequestedPoints] int,
[Status] varchar(20) NOT NULL,
[ReviewerUserId] bigint,
[ReviewNote] nvarchar(1500),
[CreatedAt] datetime2(0) NOT NULL,
[ReviewedAt] datetime2(0),
PRIMARY KEY ([ApplicationId])
);

CREATE TABLE [dbo].[PersonalEquipmentLists] (
[ListId] bigint NOT NULL IDENTITY(1,1),
[MemberId] bigint NOT NULL,
[MountainId] bigint NOT NULL,
[ListName] nvarchar(100) NOT NULL,
[HikingDate] date NOT NULL,
[HikingDays] int NOT NULL,
[Season] nvarchar(20) NOT NULL,
[IntensityLevel] nvarchar(20),
[ExperienceLevel] nvarchar(20),
[BodyWeightKg] decimal(5,2) NOT NULL,
[MaxCarryWeightGram] int NOT NULL,
[TotalWeightGram] int DEFAULT ((0)) NOT NULL,
[RemainingWeightGram] int DEFAULT ((0)) NOT NULL,
[WeightPercentage] decimal(6,2) NOT NULL,
[WeightStatus] nvarchar(20) NOT NULL,
[IsDeleted] bit DEFAULT ((0)) NOT NULL,
[CreatedAt] datetime2(7) NOT NULL,
[UpdatedAt] datetime2(7),
[RowVersion] rowversion NOT NULL,
PRIMARY KEY ([ListId])
);

CREATE TABLE [dbo].[user_achievements] (
[user_id] bigint NOT NULL,
[achievement_id] bigint NOT NULL,
[unlocked_at] datetime NOT NULL,
PRIMARY KEY ([user_id], [achievement_id])
);

CREATE TABLE [dbo].[users] (
[user_id] bigint NOT NULL IDENTITY(1,1),
[role] varchar(20) NOT NULL,
[nickname] varchar(20) NOT NULL,
[email] varchar(255) NOT NULL,
[password_hash] varchar(255) NOT NULL,
[account_status] varchar(20),
[avatar_url] varchar(255),
[avatar_blur_state] varchar(20),
[bio] varchar(500),
[current_level_id] bigint NOT NULL,
[total_xp] int NOT NULL,
[region_preference] varchar(100),
[difficulty_preference] varchar(50),
[created_at] datetime NOT NULL,
[last_active_at] datetime NOT NULL,
[displayed_achievement_id] bigint,
PRIMARY KEY ([user_id])
);

CREATE TABLE [dbo].[Tag] (
[Tag_ID] int NOT NULL IDENTITY(1,1),
[TagName] nvarchar(30) NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
PRIMARY KEY ([Tag_ID])
);

CREATE TABLE [dbo].[spatial_ref_sys] (
[srid] int NOT NULL,
[auth_name] varchar(256),
[auth_srid] int,
[srtext] varchar(2048),
[proj4text] varchar(2048),
PRIMARY KEY ([srid])
);

CREATE TABLE [dbo].[hike_record_details] (
[hikerec_detail_id] bigint NOT NULL IDENTITY(1,1),
[hike_record_id] bigint NOT NULL,
[Trail_Id] bigint NOT NULL,
[StartDate] date NOT NULL,
[EndDate] date NOT NULL,
[Status] varchar(20) NOT NULL,
[UploadedTrack] sys.geography,
[CalculatedRiskScore] decimal(6,2),
PRIMARY KEY ([hikerec_detail_id])
);

CREATE TABLE [dbo].[Category] (
[Category_ID] int NOT NULL IDENTITY(1,1),
[CategoryName] nvarchar(30) NOT NULL,
PRIMARY KEY ([Category_ID])
);

CREATE TABLE [dbo].[ArticleLike] (
[Like_ID] int NOT NULL IDENTITY(1,1),
[User_ID] bigint NOT NULL,
[Article_ID] int NOT NULL,
[CreatedDate] datetime DEFAULT (getdate()) NOT NULL,
PRIMARY KEY ([Like_ID])
);

CREATE TABLE [dbo].[TrailSubscriptions] (
[SubscriptionId] bigint NOT NULL IDENTITY(1,1),
[UserId] bigint NOT NULL,
[Trail_Id] bigint NOT NULL,
[NotifyLegalChange] bit NOT NULL,
[NotifyDisasterAlert] bit NOT NULL,
[NotifyNewReport] bit NOT NULL,
[IsActive] bit NOT NULL,
PRIMARY KEY ([SubscriptionId])
);

CREATE TABLE [dbo].[Event_Registration_and_Member_List] (
[Sign_Up_Id] bigint NOT NULL IDENTITY(1,1),
[User_Id] bigint NOT NULL,
[Event_Id] bigint NOT NULL,
[Registration_Status] int NOT NULL,
[Emergency_Contact] nvarchar(100) NOT NULL,
[Created_At] datetime NOT NULL,
PRIMARY KEY ([Sign_Up_Id])
);

CREATE TABLE [dbo].[AlertSegments] (
[AlertSegmentId] bigint NOT NULL IDENTITY(1,1),
[AlertId] bigint NOT NULL,
[SegmentName] nvarchar(200),
[Shape] sys.geography NOT NULL,
[SourceFeatureId] nvarchar(100),
[SegmentLevel] tinyint,
[Description] nvarchar(1000),
PRIMARY KEY ([AlertSegmentId])
);

CREATE TABLE [dbo].[DisasterAlerts] (
[AlertId] bigint NOT NULL IDENTITY(1,1),
[AlertType] varchar(30) NOT NULL,
[AlertTitle] nvarchar(180) NOT NULL,
[AlertDescription] nvarchar(2000),
[SeverityLevel] tinyint NOT NULL,
[EffectiveFrom] datetime2(0) NOT NULL,
[EffectiveTo] datetime2(0),
[SourceAgency] nvarchar(150),
[SourceUrl] nvarchar(1000),
[IsActive] bit NOT NULL,
PRIMARY KEY ([AlertId])
);


ALTER TABLE [dbo].[Notification]
ADD CONSTRAINT [FK_Notification_User]
FOREIGN KEY ([User_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Notification]
ADD CONSTRAINT [FK_Notification_SenderUser]
FOREIGN KEY ([Sender_User_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Notification]
ADD CONSTRAINT [FK_Notification_Article]
FOREIGN KEY ([Article_ID]) 
REFERENCES [dbo].[Article]([Article_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Notification]
ADD CONSTRAINT [FK_Notification_Comment]
FOREIGN KEY ([Comment_ID]) 
REFERENCES [dbo].[Comment]([Comment_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[group_attendance]
ADD CONSTRAINT [FK_group_attendance_group_id]
FOREIGN KEY ([group_id]) 
REFERENCES [dbo].[Event_Data]([Event_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[group_attendance]
ADD CONSTRAINT [FK_group_attendance_user_id]
FOREIGN KEY ([user_id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[skill_tags]
ADD CONSTRAINT [FK_skill_tags_parent_tag_id]
FOREIGN KEY ([parent_tag_id]) 
REFERENCES [dbo].[skill_tags]([tag_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[RefreshTokens]
ADD CONSTRAINT [FK_RefreshTokens_UserId]
FOREIGN KEY ([UserId]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Event_Data]
ADD CONSTRAINT [FK_Event_Data_Mountain_Id]
FOREIGN KEY ([Mountain_Id]) 
REFERENCES [dbo].[Mountains]([Mountain_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TrailFeatures]
ADD CONSTRAINT [FK_TrailFeatures_Trail_Id]
FOREIGN KEY ([Trail_Id]) 
REFERENCES [dbo].[Trails]([Trail_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Notify]
ADD CONSTRAINT [FK_Notify_User_Id]
FOREIGN KEY ([User_Id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[CommentImage]
ADD CONSTRAINT [FK_CommentImage_Comment]
FOREIGN KEY ([Comment_ID]) 
REFERENCES [dbo].[Comment]([Comment_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ArticleView]
ADD CONSTRAINT [FK_ArticleView_User]
FOREIGN KEY ([User_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ArticleView]
ADD CONSTRAINT [FK_ArticleView_Article]
FOREIGN KEY ([Article_ID]) 
REFERENCES [dbo].[Article]([Article_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Favorite]
ADD CONSTRAINT [FK_Favorite_User]
FOREIGN KEY ([User_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Favorite]
ADD CONSTRAINT [FK_Favorite_Article]
FOREIGN KEY ([Article_ID]) 
REFERENCES [dbo].[Article]([Article_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[MountainEquipmentSuggestions]
ADD CONSTRAINT [FK_MountainEquipmentSuggestions_EquipmentId]
FOREIGN KEY ([EquipmentId]) 
REFERENCES [dbo].[Equipments]([EquipmentId])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[MountainEquipmentSuggestions]
ADD CONSTRAINT [FK_MountainEquipmentSuggestions_MountainId]
FOREIGN KEY ([MountainId]) 
REFERENCES [dbo].[Mountains]([Mountain_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ArticleImage]
ADD CONSTRAINT [FK_ArticleImage_Article]
FOREIGN KEY ([Article_ID]) 
REFERENCES [dbo].[Article]([Article_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[user_skill_tags]
ADD CONSTRAINT [FK_user_skill_tags_user_id]
FOREIGN KEY ([user_id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[user_skill_tags]
ADD CONSTRAINT [FK_user_skill_tags_tag_id]
FOREIGN KEY ([tag_id]) 
REFERENCES [dbo].[skill_tags]([tag_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TripReports]
ADD CONSTRAINT [FK_TripReports_ReporterUserId]
FOREIGN KEY ([ReporterUserId]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TripReports]
ADD CONSTRAINT [FK_TripReports_TrId]
FOREIGN KEY ([Trail_Id]) 
REFERENCES [dbo].[Trails]([Trail_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TripReports]
ADD CONSTRAINT [FK_TripReports_ReviewedByUserId]
FOREIGN KEY ([ReviewedByUserId]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Equipments]
ADD CONSTRAINT [FK_Equipments_CategoryId]
FOREIGN KEY ([CategoryId]) 
REFERENCES [dbo].[EquipmentCategories]([CategoryId])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Report]
ADD CONSTRAINT [FK_Report_User]
FOREIGN KEY ([User_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Report]
ADD CONSTRAINT [FK_Report_Admin]
FOREIGN KEY ([Admin_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Report]
ADD CONSTRAINT [FK_Report_Article]
FOREIGN KEY ([Article_ID]) 
REFERENCES [dbo].[Article]([Article_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[EventLeader_Rating]
ADD CONSTRAINT [FK_EventLeader_Rating_Event_Id]
FOREIGN KEY ([Event_Id]) 
REFERENCES [dbo].[Event_Data]([Event_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[EventLeader_Rating]
ADD CONSTRAINT [FK_EventLeader_Rating_User_Id]
FOREIGN KEY ([User_Id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Suspension_Schedule]
ADD CONSTRAINT [FK_Suspension_Schedule_Event_Id]
FOREIGN KEY ([Event_Id]) 
REFERENCES [dbo].[Event_Data]([Event_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Suspension_Schedule]
ADD CONSTRAINT [FK_Suspension_Schedule_User_Id]
FOREIGN KEY ([User_Id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[PersonalEquipmentDetails]
ADD CONSTRAINT [FK_PersonalEquipmentDetails_EquipmentId]
FOREIGN KEY ([EquipmentId]) 
REFERENCES [dbo].[Equipments]([EquipmentId])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[PersonalEquipmentDetails]
ADD CONSTRAINT [FK_PersonalEquipmentDetails_ListId]
FOREIGN KEY ([ListId]) 
REFERENCES [dbo].[PersonalEquipmentLists]([ListId])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TrailSegments]
ADD CONSTRAINT [FK_TrailSegments_Trails]
FOREIGN KEY ([Trail_Id]) 
REFERENCES [dbo].[Trails]([Trail_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[IndicatorSegments]
ADD CONSTRAINT [FK_IndicatorSegments_Indicators]
FOREIGN KEY ([IndicatorId]) 
REFERENCES [dbo].[Indicators]([IndicatorId])
ON DELETE CASCADE
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[AlertsTrails]
ADD CONSTRAINT [FK_AlertsTrails_Alert_Id]
FOREIGN KEY ([AlertId]) 
REFERENCES [dbo].[DisasterAlerts]([AlertId])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[AlertsTrails]
ADD CONSTRAINT [FK_AlertsTrails_Trail_Id]
FOREIGN KEY ([Trail_Id]) 
REFERENCES [dbo].[Trails]([Trail_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Comment]
ADD CONSTRAINT [FK_Comment_Parent]
FOREIGN KEY ([ParentComment_ID]) 
REFERENCES [dbo].[Comment]([Comment_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Comment]
ADD CONSTRAINT [FK_Comment_ReplyUser]
FOREIGN KEY ([ReplyToUser_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Comment]
ADD CONSTRAINT [FK_Comment_Article]
FOREIGN KEY ([Article_ID]) 
REFERENCES [dbo].[Article]([Article_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Comment]
ADD CONSTRAINT [FK_Comment_User]
FOREIGN KEY ([User_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ArticleTag]
ADD CONSTRAINT [FK_ArticleTag_Article]
FOREIGN KEY ([Article_ID]) 
REFERENCES [dbo].[Article]([Article_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ArticleTag]
ADD CONSTRAINT [FK_ArticleTag_Tag]
FOREIGN KEY ([Tag_ID]) 
REFERENCES [dbo].[Tag]([Tag_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TrailIndicators]
ADD CONSTRAINT [FK_TrailIndicators_Trails]
FOREIGN KEY ([Trail_Id]) 
REFERENCES [dbo].[Trails]([Trail_Id])
ON DELETE CASCADE
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TrailIndicators]
ADD CONSTRAINT [FK_TrailIndicators_Indicators]
FOREIGN KEY ([IndicatorId]) 
REFERENCES [dbo].[Indicators]([IndicatorId])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Event_Report_Complaint]
ADD CONSTRAINT [FK_Event_Report_Complaint_User_Id]
FOREIGN KEY ([User_Id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Event_Report_Complaint]
ADD CONSTRAINT [FK_Event_Report_Complaint_Event_Id]
FOREIGN KEY ([Event_Id]) 
REFERENCES [dbo].[Event_Data]([Event_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Article]
ADD CONSTRAINT [FK_Article_Category]
FOREIGN KEY ([Category_ID]) 
REFERENCES [dbo].[Category]([Category_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Article]
ADD CONSTRAINT [FK_Article_User]
FOREIGN KEY ([User_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[hike_records]
ADD CONSTRAINT [FK_hike_records_mountain_id]
FOREIGN KEY ([mountain_id]) 
REFERENCES [dbo].[Mountains]([Mountain_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[hike_records]
ADD CONSTRAINT [FK_hike_records_user_id]
FOREIGN KEY ([user_id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ReviewApplications]
ADD CONSTRAINT [FK_ReviewApplications_ApplicantUserId]
FOREIGN KEY ([ApplicantUserId]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ReviewApplications]
ADD CONSTRAINT [FK_ReviewApplications_RelatedReportId]
FOREIGN KEY ([RelatedReportId]) 
REFERENCES [dbo].[TripReports]([ReportId])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ReviewApplications]
ADD CONSTRAINT [FK_ReviewApplications_Related_Hike_DRecord_Id]
FOREIGN KEY ([Related_Hike_DRecord_Id]) 
REFERENCES [dbo].[hike_record_details]([hikerec_detail_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ReviewApplications]
ADD CONSTRAINT [FK_ReviewApplications_ReviewerUserId]
FOREIGN KEY ([ReviewerUserId]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[PersonalEquipmentLists]
ADD CONSTRAINT [FK_PersonalEquipmentLists_MemberId]
FOREIGN KEY ([MemberId]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[PersonalEquipmentLists]
ADD CONSTRAINT [FK_PersonalEquipmentLists_MountainId]
FOREIGN KEY ([MountainId]) 
REFERENCES [dbo].[Mountains]([Mountain_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[user_achievements]
ADD CONSTRAINT [FK_user_achievements_user_id]
FOREIGN KEY ([user_id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[user_achievements]
ADD CONSTRAINT [FK_user_achievements_achievement_id]
FOREIGN KEY ([achievement_id]) 
REFERENCES [dbo].[achievements]([achievement_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[users]
ADD CONSTRAINT [FK_users_displayed_achievement_id]
FOREIGN KEY ([displayed_achievement_id]) 
REFERENCES [dbo].[achievements]([achievement_id])
ON DELETE SET NULL
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[users]
ADD CONSTRAINT [FK_users_current_level_id]
FOREIGN KEY ([current_level_id]) 
REFERENCES [dbo].[levels]([level_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[hike_record_details]
ADD CONSTRAINT [FK_hike_record_details_hike_record_id]
FOREIGN KEY ([hike_record_id]) 
REFERENCES [dbo].[hike_records]([record_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[hike_record_details]
ADD CONSTRAINT [FK_hike_record_details_Trail_Id]
FOREIGN KEY ([Trail_Id]) 
REFERENCES [dbo].[Trails]([Trail_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ArticleLike]
ADD CONSTRAINT [FK_ArticleLike_Article]
FOREIGN KEY ([Article_ID]) 
REFERENCES [dbo].[Article]([Article_ID])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[ArticleLike]
ADD CONSTRAINT [FK_ArticleLike_User]
FOREIGN KEY ([User_ID]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TrailSubscriptions]
ADD CONSTRAINT [FK_TrailSubscriptions_Trail_Id]
FOREIGN KEY ([Trail_Id]) 
REFERENCES [dbo].[Trails]([Trail_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[TrailSubscriptions]
ADD CONSTRAINT [FK_TrailSubscriptions_UserId]
FOREIGN KEY ([UserId]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Event_Registration_and_Member_List]
ADD CONSTRAINT [FK_Event_Registration_and_Member_List_Event_Id]
FOREIGN KEY ([Event_Id]) 
REFERENCES [dbo].[Event_Data]([Event_Id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[Event_Registration_and_Member_List]
ADD CONSTRAINT [FK_Event_Registration_and_Member_List_User_Id]
FOREIGN KEY ([User_Id]) 
REFERENCES [dbo].[users]([user_id])
ON DELETE NO ACTION
ON UPDATE NO ACTION;



ALTER TABLE [dbo].[AlertSegments]
ADD CONSTRAINT [FK_AlertSegments_DisasterAlerts]
FOREIGN KEY ([AlertId]) 
REFERENCES [dbo].[DisasterAlerts]([AlertId])
ON DELETE NO ACTION
ON UPDATE NO ACTION;
