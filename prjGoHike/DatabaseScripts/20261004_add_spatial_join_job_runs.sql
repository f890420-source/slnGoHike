/*
用途：空間關聯背景工作的最小 schema 異動。
基底：20260930_dbsnapshot_gohikesave.sql。
規劃：../docs/Hangfire_空間關聯最小異動建議.md 第 2 節。

請在已選定的目標資料庫，以獨立批次、無外層交易執行。
本腳本不硬編碼資料庫名稱；部署前確認 DB_NAME() 與基底 schema。
可於本腳本建立的 schema 上重複執行；不修復其他來源的同名物件定義。
既有分數（包括 0）、距離、權重與時間全部保留，不執行空間重算。

啟用 Job 前須同步 EF Model：EvaluatedScore 改為 decimal?，
並新增 BackgroundJobRun、DbSet 與映射；本腳本不啟用 Hangfire。
不建立 HangFire.*、TrailPoi、空間索引或排程，也不變更 EvaluatedAt 預設值。
若後續已有 NULL 評分列，不可直接還原為 NOT NULL 或填 0 回復。
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- 只接受基底的 decimal(10,4)，避免 ALTER COLUMN 意外轉換其他型別。
    IF NOT EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.TrailIndicators', N'U')
          AND name = N'EvaluatedScore'
          AND user_type_id = TYPE_ID(N'decimal')
          AND precision = 10 AND scale = 4
          AND is_computed = 0
    )
        THROW 51100, 'Expected dbo.TrailIndicators.EvaluatedScore decimal(10,4).', 1;

    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.TrailIndicators', N'U')
          AND name = N'EvaluatedScore' AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE dbo.TrailIndicators
        ALTER COLUMN EvaluatedScore decimal(10,4) NULL;
    END;

    IF OBJECT_ID(N'dbo.BackgroundJobRuns') IS NOT NULL
       AND OBJECT_ID(N'dbo.BackgroundJobRuns', N'U') IS NULL
        THROW 51101, 'dbo.BackgroundJobRuns exists but is not a user table.', 1;

    IF OBJECT_ID(N'dbo.BackgroundJobRuns', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BackgroundJobRuns (
            Id bigint IDENTITY(1,1) NOT NULL,
            JobType varchar(50) NOT NULL,
            Status varchar(20) NOT NULL,
            HangfireJobId varchar(100) NULL,
            DistanceMeters decimal(12,2) NOT NULL,
            CreatedAt datetime2(0) NOT NULL
                CONSTRAINT DF_BackgroundJobRuns_CreatedAt DEFAULT (SYSUTCDATETIME()),
            StartedAt datetime2(0) NULL,
            FinishedAt datetime2(0) NULL,
            ErrorMessage nvarchar(500) NULL,
            CONSTRAINT PK_BackgroundJobRuns PRIMARY KEY (Id),
            CONSTRAINT CK_BackgroundJobRuns_Status CHECK (
                Status IN ('Pending', 'Queued', 'Processing', 'RetryPending', 'Succeeded', 'Failed')
            ),
            CONSTRAINT CK_BackgroundJobRuns_DistanceMeters CHECK (
                DistanceMeters >= 0.01 AND DistanceMeters <= 10000.00
            )
        );
    END;

    -- 同名表若欄位不同則停止，避免將不相容 schema 誤當成已部署。
    -- max_length 以 byte 計，nvarchar(500) 為 1000；datetime2 以型別與 scale 核對。
    IF EXISTS (
        SELECT 1
        FROM (VALUES
            (N'Id',              N'bigint',    8,    0, 0,  1),
            (N'JobType',         N'varchar',   50,   0, 0,  0),
            (N'Status',          N'varchar',   20,   0, 0,  0),
            (N'HangfireJobId',   N'varchar',   100,  1, 0,  0),
            (N'DistanceMeters', N'decimal',   9,    0, 2,  0),
            (N'CreatedAt',      N'datetime2', NULL, 0, 0,  0),
            (N'StartedAt',      N'datetime2', NULL, 1, 0,  0),
            (N'FinishedAt',     N'datetime2', NULL, 1, 0,  0),
            (N'ErrorMessage',   N'nvarchar',  1000, 1, 0,  0)
        ) AS expected(ColumnName, TypeName, MaxLength, IsNullable, Scale, IsIdentity)
        LEFT JOIN sys.columns AS actual
          ON actual.object_id = OBJECT_ID(N'dbo.BackgroundJobRuns', N'U')
         AND actual.name = expected.ColumnName
        WHERE actual.column_id IS NULL
           OR actual.user_type_id <> TYPE_ID(expected.TypeName)
           OR (expected.MaxLength IS NOT NULL AND actual.max_length <> expected.MaxLength)
           OR actual.is_nullable <> expected.IsNullable
           OR actual.scale <> expected.Scale
           OR actual.is_identity <> expected.IsIdentity
           OR actual.is_computed <> 0
           OR (expected.ColumnName = N'DistanceMeters' AND actual.precision <> 12)
    )
        THROW 51102, 'dbo.BackgroundJobRuns columns differ from the spatial join plan.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.BackgroundJobRuns', N'U')
          AND name = N'IX_BackgroundJobRuns_JobType_Status_CreatedAt'
    )
    BEGIN
        CREATE INDEX IX_BackgroundJobRuns_JobType_Status_CreatedAt
        ON dbo.BackgroundJobRuns(JobType, Status, CreatedAt);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- 唯讀摘要；部署後仍須依文件核對 CHECK、預設值與索引定義。
SELECT DB_NAME() AS DatabaseName, t.name AS TableName, c.name AS ColumnName,
       TYPE_NAME(c.user_type_id) AS SqlType, c.max_length, c.precision, c.scale,
       c.is_nullable, c.is_identity
FROM sys.tables AS t
JOIN sys.columns AS c ON c.object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID(N'dbo')
  AND (t.name = N'BackgroundJobRuns'
    OR (t.name = N'TrailIndicators' AND c.name = N'EvaluatedScore'))
ORDER BY t.name, c.column_id;
