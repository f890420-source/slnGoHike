SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Mountains', N'U') IS NULL
        OR OBJECT_ID(N'dbo.EquipmentCategories', N'U') IS NULL
        OR OBJECT_ID(N'dbo.Equipments', N'U') IS NULL
        OR OBJECT_ID(N'dbo.MountainEquipmentSuggestions', N'U') IS NULL
    BEGIN
        THROW 50001, N'缺少個人裝備建議所需的資料表，已停止執行。', 1;
    END;

    /* 補齊裝備分類；既有分類不覆寫。 */
    INSERT INTO dbo.EquipmentCategories
        (CategoryName, SortOrder, IsActive, CreatedAt)
    SELECT source.CategoryName, source.SortOrder, 1, SYSUTCDATETIME()
    FROM (VALUES
        (N'飲水補給', 6),
        (N'導航通訊', 7),
        (N'急救求生', 8),
        (N'糧食補給', 9),
        (N'技術裝備', 10)
    ) AS source(CategoryName, SortOrder)
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.EquipmentCategories AS target
        WHERE target.CategoryName = source.CategoryName
    );

    /* 補齊裝備主檔；重量為個人配重估算用的參考值。 */
    DECLARE @EquipmentSeed TABLE
    (
        CategoryName nvarchar(50) NOT NULL,
        EquipmentName nvarchar(100) NOT NULL,
        StandardWeightGram int NOT NULL,
        RequirementLevel nvarchar(20) NOT NULL,
        Description nvarchar(500) NULL
    );

    INSERT INTO @EquipmentSeed
        (CategoryName, EquipmentName, StandardWeightGram,
         RequirementLevel, Description)
    VALUES
        (N'背負系統', N'65L 登山背包', 2100, N'必備',
         N'適合多日行程；實際容量須依住宿、炊煮與團體裝備調整。'),
        (N'睡眠系統', N'睡袋（-5度）', 1200, N'必備',
         N'多日行程睡眠保暖；舒適溫標須依季節、海拔與住宿環境調整。'),
        (N'衣著防護', N'雨衣（外套）', 350, N'必備',
         N'防風、防雨並降低失溫風險；高山應選擇合身且可活動的防水外層。'),
        (N'衣著防護', N'雨衣（褲子）', 250, N'建議',
         N'中高難度或長時間暴露路線建議搭配防水外套，避免下半身濕冷。'),
        (N'照明安全', N'頭燈', 100, N'必備',
         N'即使規劃白天往返，也應預防摸黑或行程延誤。'),
        (N'登山輔具', N'登山杖', 500, N'建議',
         N'協助平衡並降低膝蓋負擔；岩稜或需手腳並用地形應適時收起。'),
        (N'飲水補給', N'水壺／水袋', 200, N'必備',
         N'盛裝飲水；出發前應確認路線水源、補水點與淨水方式。'),
        (N'飲水補給', N'飲用水（1 公升）', 1000, N'必備',
         N'每單位代表 1 公升飲水；實際攜帶量須依氣溫、強度、時間與補水點調整。'),
        (N'飲水補給', N'電解質補充品', 100, N'建議',
         N'高強度或大量流汗時補充；不能取代正常飲水與飲食。'),
        (N'衣著防護', N'登山鞋', 900, N'必備',
         N'選擇已磨合、具抓地力且適合路況的登山鞋或健行鞋。'),
        (N'衣著防護', N'保暖中層／外套', 500, N'必備',
         N'採洋蔥式穿搭；高山即使夏季也可能低溫，須依預報與個人耐寒度調整。'),
        (N'衣著防護', N'保暖帽與手套', 200, N'建議',
         N'降低頭部與手部失溫風險，高山、冬季或強風環境尤其重要。'),
        (N'衣著防護', N'遮陽帽與防曬用品', 150, N'建議',
         N'減少曝曬與紫外線傷害；高海拔仍應加強防曬。'),
        (N'衣著防護', N'備用乾燥衣物', 500, N'建議',
         N'多日行程或衣物濕透時更換，並以防水袋獨立收納。'),
        (N'導航通訊', N'手機與離線地圖', 250, N'必備',
         N'行前下載離線地圖及路線資料；手機不能取代完整行程規劃。'),
        (N'導航通訊', N'行動電源', 300, N'建議',
         N'維持導航與緊急通訊電力；低溫時注意電池耗電較快。'),
        (N'導航通訊', N'紙本路線資料與防水袋', 100, N'建議',
         N'電子設備失效時備援使用，出發前先熟悉路線與撤退點。'),
        (N'急救求生', N'個人急救包', 300, N'必備',
         N'包含個人藥品及基本傷口處理用品；應熟悉內容物的正確用法。'),
        (N'急救求生', N'緊急保暖毯', 60, N'必備',
         N'緊急停留或失溫風險時提供基本保溫與遮蔽。'),
        (N'急救求生', N'求生哨', 20, N'必備',
         N'迷途或緊急狀況下發出聲音訊號，應放在隨手可取處。'),
        (N'急救求生', N'身分證與健保卡', 20, N'必備',
         N'供入山入園查驗及緊急醫療辨識使用，建議防水收納。'),
        (N'照明安全', N'頭燈備用電池', 100, N'必備',
         N'供頭燈備援；出發前確認頭燈與電池均可正常使用。'),
        (N'糧食補給', N'行動糧與預備糧', 500, N'必備',
         N'準備易取用、高熱量食物，並保留緊急延誤時的預備份量。'),
        (N'技術裝備', N'冰爪', 800, N'建議',
         N'僅在積雪或結冰路況且具備正確操作能力時使用；行前確認雪況。'),
        (N'技術裝備', N'冰斧', 500, N'建議',
         N'冬季高山技術裝備，須經訓練並配合實際雪況及路線風險使用。'),
        (N'技術裝備', N'太陽眼鏡／雪鏡', 100, N'建議',
         N'高海拔強光或雪地環境保護眼睛，雪地應選擇足夠防護等級。');

    IF EXISTS (
        SELECT EquipmentName
        FROM dbo.Equipments
        GROUP BY EquipmentName
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 50002, N'裝備主檔存在同名資料，為避免錯誤關聯已停止執行。', 1;
    END;

    INSERT INTO dbo.Equipments
        (CategoryId, EquipmentName, StandardWeightGram,
         RequirementLevel, Description, ImageUrl, IsActive, UpdatedAt)
    SELECT category.CategoryId,
           seed.EquipmentName,
           seed.StandardWeightGram,
           seed.RequirementLevel,
           seed.Description,
           NULL,
           1,
           SYSUTCDATETIME()
    FROM @EquipmentSeed AS seed
    INNER JOIN dbo.EquipmentCategories AS category
        ON category.CategoryName = seed.CategoryName
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Equipments AS target
        WHERE target.EquipmentName = seed.EquipmentName
    );

    /*
       Min/MaxDifficulty 與 Min/MaxAltitude 用來套用山岳層級規則。
       條件為 NULL 時代表不限；季節、強度、經驗沿用既有 API 的比對方式。
    */
    DECLARE @Rules TABLE
    (
        EquipmentName nvarchar(100) NOT NULL,
        Season nvarchar(20) NOT NULL,
        MinimumDays int NOT NULL,
        MaximumDays int NULL,
        IntensityLevel nvarchar(20) NULL,
        ExperienceLevel nvarchar(20) NULL,
        SuggestedQuantity int NOT NULL,
        RequirementLevel nvarchar(20) NOT NULL,
        Notes nvarchar(500) NULL,
        MinDifficulty int NULL,
        MaxDifficulty int NULL,
        MinAltitude int NULL,
        MaxAltitude int NULL
    );

    INSERT INTO @Rules VALUES
        /* 所有路線的安全基礎。 */
        (N'雨衣（外套）', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'山區天氣變化快，作為防風、防雨與降低失溫風險的外層。', NULL, NULL, NULL, NULL),
        (N'頭燈', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'即使規劃白天往返，也應預防摸黑或行程延誤。', NULL, NULL, NULL, NULL),
        (N'頭燈備用電池', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'與頭燈分開防水收納，出發前確認電量。', NULL, NULL, NULL, NULL),
        (N'水壺／水袋', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'先確認沿途水源與補水點；生水須經可靠方式處理。', NULL, NULL, NULL, NULL),
        (N'飲用水（1 公升）', N'全年', 1, NULL, NULL, NULL, 2, N'必備',
         N'以 2 公升作基準估重；請依氣溫、強度、體質、路程及補水點增減。', NULL, NULL, NULL, NULL),
        (N'登山鞋', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'穿著已磨合且適合路況的鞋款，雨後或岩稜路線更要注意抓地力。', NULL, NULL, NULL, NULL),
        (N'手機與離線地圖', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'預先下載離線地圖，並將行程與留守資訊交給親友。', NULL, NULL, NULL, NULL),
        (N'個人急救包', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'攜帶個人藥品及基本傷口處理用品，且應會正確使用。', NULL, NULL, NULL, NULL),
        (N'緊急保暖毯', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'因應受傷、迷途、失溫或被迫等待救援。', NULL, NULL, NULL, NULL),
        (N'求生哨', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'放在肩帶等可立即取用位置，供迷途或緊急求援。', NULL, NULL, NULL, NULL),
        (N'身分證與健保卡', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'以防水袋收納；另確認路線是否需要入山或入園許可。', NULL, NULL, NULL, NULL),
        (N'行動糧與預備糧', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'除預定飲食外，保留因行程延誤所需的預備糧。', NULL, NULL, NULL, NULL),

        /* 多日行程。 */
        (N'65L 登山背包', N'全年', 2, NULL, NULL, NULL, 1, N'必備',
         N'多日行程使用；容量仍須依住宿、炊煮與團體裝備調整。', NULL, NULL, NULL, NULL),
        (N'睡袋（-5度）', N'全年', 2, NULL, NULL, NULL, 1, N'必備',
         N'多日行程睡眠保暖；溫標須依季節、海拔與住宿環境調整。', NULL, NULL, NULL, NULL),
        (N'備用乾燥衣物', N'全年', 2, NULL, NULL, NULL, 1, N'建議',
         N'多日行程備用，使用防水袋保持乾燥。', NULL, NULL, NULL, NULL),

        /* 難度與路程風險。 */
        (N'登山杖', N'全年', 1, NULL, NULL, NULL, 1, N'建議',
         N'協助平衡並降低膝蓋負擔。', 1, 3, NULL, NULL),
        (N'登山杖', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'長距離或高難度路線建議攜帶，仍須依岩稜地形判斷何時收起。', 4, NULL, NULL, NULL),
        (N'雨衣（褲子）', N'全年', 1, NULL, NULL, NULL, 1, N'建議',
         N'中高難度或長時間暴露路線建議搭配防水外套。', 3, NULL, NULL, NULL),
        (N'行動電源', N'全年', 1, NULL, NULL, NULL, 1, N'必備',
         N'維持離線導航與緊急通訊電力；低溫時注意保暖與耗電。', 3, NULL, NULL, NULL),

        /* 季節差異：低海拔夏季不加入保暖外套，高山夏季仍保留。 */
        (N'遮陽帽與防曬用品', N'夏季', 1, NULL, NULL, NULL, 1, N'建議',
         N'夏季日照強，沿途應持續補充水分並避免熱傷害。', NULL, NULL, NULL, NULL),
        (N'保暖中層／外套', N'冬季', 1, NULL, NULL, NULL, 1, N'必備',
         N'冬季採排汗、保暖、防風防水的分層穿著，保持衣物乾燥。', NULL, NULL, NULL, NULL),
        (N'保暖帽與手套', N'冬季', 1, NULL, NULL, NULL, 1, N'必備',
         N'保護頭部與末梢，強風或濕冷時可降低失溫風險。', NULL, NULL, NULL, NULL),
        (N'保暖中層／外套', N'春季', 1, NULL, NULL, NULL, 1, N'必備',
         N'3,000 公尺以上溫差大，即使非冬季也需保暖層。', NULL, NULL, 3000, NULL),
        (N'保暖中層／外套', N'秋季', 1, NULL, NULL, NULL, 1, N'必備',
         N'3,000 公尺以上溫差大，即使非冬季也需保暖層。', NULL, NULL, 3000, NULL),
        (N'保暖中層／外套', N'夏季', 1, NULL, NULL, NULL, 1, N'建議',
         N'高山夏季仍可能低溫、強風或降雨，建議保留輕量保暖層。', NULL, NULL, 3000, NULL),
        (N'保暖帽與手套', N'春季', 1, NULL, NULL, NULL, 1, N'建議',
         N'高山稜線風強，依預報及個人耐寒程度攜帶。', NULL, NULL, 3000, NULL),
        (N'保暖帽與手套', N'秋季', 1, NULL, NULL, NULL, 1, N'建議',
         N'高山稜線風強，依預報及個人耐寒程度攜帶。', NULL, NULL, 3000, NULL),
        (N'保暖帽與手套', N'夏季', 1, NULL, NULL, NULL, 1, N'建議',
         N'高山夏季仍有低溫與強風風險，作為輕量備援。', NULL, NULL, 3000, NULL),

        /* 高海拔及冬季技術裝備。 */
        (N'太陽眼鏡／雪鏡', N'全年', 1, NULL, NULL, NULL, 1, N'建議',
         N'高海拔紫外線較強；雪地須使用足夠防護等級的雪鏡。', NULL, NULL, 3000, NULL),
        (N'冰爪', N'冬季', 1, NULL, NULL, NULL, 1, N'建議',
         N'不是看到冬季就一定使用；須先查雪況，並具備正確穿戴與行走技術。', NULL, NULL, 3000, NULL),
        (N'冰斧', N'冬季', 1, NULL, NULL, NULL, 1, N'建議',
         N'高難度冬季高山依雪況與路線評估；未受訓者不應以攜帶裝備取代技術。', 4, NULL, 3000, NULL),

        /* 使用者條件差異。 */
        (N'電解質補充品', N'全年', 1, NULL, N'高強度', NULL, 1, N'建議',
         N'大量流汗時補充，仍須正常飲水與進食。', NULL, NULL, NULL, NULL),
        (N'紙本路線資料與防水袋', N'全年', 1, NULL, NULL, N'新手', 1, N'建議',
         N'作為電子導航備援；出發前先理解路線、時間與撤退點。', NULL, NULL, NULL, NULL);

    /* 同一山岳、裝備及使用者條件只建立一筆，腳本可安全重跑。 */
    INSERT INTO dbo.MountainEquipmentSuggestions
        (MountainId, EquipmentId, Season, MinimumDays, MaximumDays,
         IntensityLevel, ExperienceLevel, SuggestedQuantity,
         RequirementLevel, Notes)
    SELECT mountain.Mountain_Id,
           equipment.EquipmentId,
           recommendation.Season,
           recommendation.MinimumDays,
           recommendation.MaximumDays,
           recommendation.IntensityLevel,
           recommendation.ExperienceLevel,
           recommendation.SuggestedQuantity,
           recommendation.RequirementLevel,
           recommendation.Notes
    FROM @Rules AS recommendation
    INNER JOIN dbo.Equipments AS equipment
        ON equipment.EquipmentName = recommendation.EquipmentName
    CROSS JOIN dbo.Mountains AS mountain
    WHERE (recommendation.MinDifficulty IS NULL
           OR mountain.Difficulty_Level >= recommendation.MinDifficulty)
      AND (recommendation.MaxDifficulty IS NULL
           OR mountain.Difficulty_Level <= recommendation.MaxDifficulty)
      AND (recommendation.MinAltitude IS NULL
           OR mountain.Altitude >= recommendation.MinAltitude)
      AND (recommendation.MaxAltitude IS NULL
           OR mountain.Altitude <= recommendation.MaxAltitude)
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.MountainEquipmentSuggestions AS existing
          WHERE existing.MountainId = mountain.Mountain_Id
            AND existing.EquipmentId = equipment.EquipmentId
            AND existing.Season = recommendation.Season
            AND existing.MinimumDays = recommendation.MinimumDays
            AND ((existing.MaximumDays = recommendation.MaximumDays)
                 OR (existing.MaximumDays IS NULL
                     AND recommendation.MaximumDays IS NULL))
            AND ((existing.IntensityLevel = recommendation.IntensityLevel)
                 OR (existing.IntensityLevel IS NULL
                     AND recommendation.IntensityLevel IS NULL))
            AND ((existing.ExperienceLevel = recommendation.ExperienceLevel)
                 OR (existing.ExperienceLevel IS NULL
                     AND recommendation.ExperienceLevel IS NULL))
      );

    /* 讓既有山岳與新山岳套用一致的詳細說明。 */
    UPDATE existing
    SET existing.SuggestedQuantity = recommendation.SuggestedQuantity,
        existing.RequirementLevel = recommendation.RequirementLevel,
        existing.Notes = recommendation.Notes
    FROM dbo.MountainEquipmentSuggestions AS existing
    INNER JOIN dbo.Equipments AS equipment
        ON equipment.EquipmentId = existing.EquipmentId
    INNER JOIN @Rules AS recommendation
        ON recommendation.EquipmentName = equipment.EquipmentName
       AND recommendation.Season = existing.Season
       AND recommendation.MinimumDays = existing.MinimumDays
       AND ((recommendation.MaximumDays = existing.MaximumDays)
            OR (recommendation.MaximumDays IS NULL
                AND existing.MaximumDays IS NULL))
       AND ((recommendation.IntensityLevel = existing.IntensityLevel)
            OR (recommendation.IntensityLevel IS NULL
                AND existing.IntensityLevel IS NULL))
       AND ((recommendation.ExperienceLevel = existing.ExperienceLevel)
            OR (recommendation.ExperienceLevel IS NULL
                AND existing.ExperienceLevel IS NULL))
    INNER JOIN dbo.Mountains AS mountain
        ON mountain.Mountain_Id = existing.MountainId
       AND (recommendation.MinDifficulty IS NULL
            OR mountain.Difficulty_Level >= recommendation.MinDifficulty)
       AND (recommendation.MaxDifficulty IS NULL
            OR mountain.Difficulty_Level <= recommendation.MaxDifficulty)
       AND (recommendation.MinAltitude IS NULL
            OR mountain.Altitude >= recommendation.MinAltitude)
       AND (recommendation.MaxAltitude IS NULL
            OR mountain.Altitude <= recommendation.MaxAltitude);

    UPDATE equipment
    SET equipment.Description = seed.Description,
        equipment.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Equipments AS equipment
    INNER JOIN @EquipmentSeed AS seed
        ON seed.EquipmentName = equipment.EquipmentName;

    IF EXISTS (
        SELECT 1
        FROM dbo.Mountains AS mountain
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.MountainEquipmentSuggestions AS suggestion
            WHERE suggestion.MountainId = mountain.Mountain_Id
        )
    )
    BEGIN
        THROW 50003, N'仍有山岳缺少裝備建議，已回復全部異動。', 1;
    END;

    COMMIT TRANSACTION;

    SELECT COUNT(*) AS EquipmentCount
    FROM dbo.Equipments;

    SELECT COUNT(*) AS SuggestionCount
    FROM dbo.MountainEquipmentSuggestions;

    SELECT COUNT(*) AS MountainsWithoutSuggestions
    FROM dbo.Mountains AS mountain
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.MountainEquipmentSuggestions AS suggestion
        WHERE suggestion.MountainId = mountain.Mountain_Id
    );
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
