-- 由呼叫端提供：@RunId bigint、@DistanceMeters decimal(12,2)。
-- 已在 Serializable 交易內取得應用程式鎖，且再次確認此 Run 可以執行。
-- 若 Run 已 Succeeded／Failed，或目前 Hangfire 工作不是該 Run 的執行者，
-- 呼叫端直接結束，不進入本片段。失敗一律 rollback，再處理工作狀態。
SET XACT_ABORT ON;

IF @DistanceMeters IS NULL OR @DistanceMeters < 0.01 OR @DistanceMeters > 10000.00
    THROW 51000, 'Invalid spatial distance threshold.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.Trails AS t
    WHERE t.IsPublished = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.TrailSegments AS s WHERE s.Trail_Id = t.Trail_Id)
) OR EXISTS (
    SELECT 1 FROM dbo.Indicators AS i
    WHERE i.IsActive = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.IndicatorSegments AS s WHERE s.IndicatorId = i.IndicatorId)
)
    THROW 51001, 'Active spatial source has no segments.', 1;

SELECT s.Trail_Id, s.Shape
INTO #TrailSource
FROM dbo.TrailSegments AS s
JOIN dbo.Trails AS t ON t.Trail_Id = s.Trail_Id
WHERE t.IsPublished = 1;

SELECT s.IndicatorId, s.Shape, i.Weight
INTO #IndicatorSource
FROM dbo.IndicatorSegments AS s
JOIN dbo.Indicators AS i ON i.IndicatorId = s.IndicatorId
WHERE i.IsActive = 1;

IF EXISTS (
    SELECT 1 FROM #TrailSource
    WHERE Shape IS NULL OR Shape.STSrid <> 4326
       OR Shape.STIsValid() = 0 OR Shape.STIsEmpty() = 1
       OR Shape.STGeometryType() NOT IN ('LineString', 'MultiLineString')
) OR EXISTS (
    SELECT 1 FROM #IndicatorSource
    WHERE Shape IS NULL OR Shape.STSrid <> 4326
       OR Shape.STIsValid() = 0 OR Shape.STIsEmpty() = 1
       OR Shape.STGeometryType() NOT IN
          ('Point', 'MultiPoint', 'LineString', 'MultiLineString', 'Polygon', 'MultiPolygon')
)
    THROW 51002, 'Invalid spatial source.', 1;

-- 一個主表配對可能有很多 Segment 配對，必須先彙總。
SELECT t.Trail_Id, i.IndicatorId,
       MIN(d.Meters) AS MinDistanceMeters,
       MAX(i.Weight) AS IndicatorWeightSnapshot,
       COUNT_BIG(*) - COUNT_BIG(d.Meters) AS NullDistanceCount
INTO #PairDistances
FROM #TrailSource AS t
CROSS JOIN #IndicatorSource AS i
CROSS APPLY (VALUES (i.Shape.STDistance(t.Shape))) AS d(Meters)
GROUP BY t.Trail_Id, i.IndicatorId;

IF EXISTS (SELECT 1 FROM #PairDistances WHERE NullDistanceCount > 0)
    THROW 51003, 'Unexpected null spatial distance.', 1;

SELECT Trail_Id, IndicatorId,
       CAST(MinDistanceMeters AS decimal(12,2)) AS DistanceMeters,
       IndicatorWeightSnapshot
INTO #Candidates
FROM #PairDistances
WHERE MinDistanceMeters <= @DistanceMeters; -- 先比較原始值，再轉 decimal。

CREATE UNIQUE CLUSTERED INDEX IX_Candidates
ON #Candidates(Trail_Id, IndicatorId);

DECLARE @EvaluatedAt datetime2(0) = SYSUTCDATETIME();

UPDATE target
SET DistanceMeters = candidate.DistanceMeters,
    IndicatorWeightSnapshot = candidate.IndicatorWeightSnapshot,
    OverlapRatio = NULL,
    RawScore = NULL,
    EvaluatedAt = @EvaluatedAt
FROM dbo.TrailIndicators AS target
JOIN #Candidates AS candidate
  ON candidate.Trail_Id = target.Trail_Id
 AND candidate.IndicatorId = target.IndicatorId
WHERE target.EvaluatedScore IS NULL;

INSERT INTO dbo.TrailIndicators
    (Trail_Id, IndicatorId, DistanceMeters, IndicatorWeightSnapshot,
     OverlapRatio, RawScore, EvaluatedScore, EvaluatedAt)
SELECT candidate.Trail_Id, candidate.IndicatorId,
       candidate.DistanceMeters, candidate.IndicatorWeightSnapshot,
       NULL, NULL, NULL, @EvaluatedAt
FROM #Candidates AS candidate
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.TrailIndicators AS target
    WHERE target.Trail_Id = candidate.Trail_Id
      AND target.IndicatorId = candidate.IndicatorId
);

DELETE target
FROM dbo.TrailIndicators AS target
WHERE target.EvaluatedScore IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM #Candidates AS candidate
      WHERE candidate.Trail_Id = target.Trail_Id
        AND candidate.IndicatorId = target.IndicatorId
  );

UPDATE dbo.BackgroundJobRuns
SET Status = 'Succeeded', FinishedAt = SYSUTCDATETIME(), ErrorMessage = NULL
WHERE Id = @RunId AND JobType = 'SpatialJoin' AND Status = 'Processing'
  AND DistanceMeters = @DistanceMeters AND HangfireJobId = @JobId;

IF @@ROWCOUNT <> 1
    THROW 51004, 'Spatial run state changed.', 1;

DROP TABLE #Candidates;
DROP TABLE #PairDistances;
DROP TABLE #IndicatorSource;
DROP TABLE #TrailSource;
-- 呼叫端 commit；上述任一步驟失敗時，呼叫端 rollback 全部資料與成功狀態。
