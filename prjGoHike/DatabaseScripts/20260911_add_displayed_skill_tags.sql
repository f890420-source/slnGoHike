/* 會員可從已解鎖技能中選擇最多三個公開展示。可重複執行。 */
IF COL_LENGTH('dbo.user_skill_tags', 'is_displayed') IS NULL
BEGIN
    ALTER TABLE dbo.user_skill_tags
    ADD is_displayed bit NOT NULL
        CONSTRAINT DF_user_skill_tags_is_displayed DEFAULT (0);
END;
