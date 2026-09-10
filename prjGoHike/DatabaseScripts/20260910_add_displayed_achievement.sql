IF COL_LENGTH('dbo.users', 'displayed_achievement_id') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD displayed_achievement_id BIGINT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_users_displayed_achievement_id'
      AND object_id = OBJECT_ID('dbo.users')
)
BEGIN
    CREATE INDEX IX_users_displayed_achievement_id
        ON dbo.users(displayed_achievement_id);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_users_displayed_achievement_id'
)
BEGIN
    ALTER TABLE dbo.users
        ADD CONSTRAINT FK_users_displayed_achievement_id
        FOREIGN KEY (displayed_achievement_id)
        REFERENCES dbo.achievements(achievement_id)
        ON DELETE SET NULL;
END;
GO
