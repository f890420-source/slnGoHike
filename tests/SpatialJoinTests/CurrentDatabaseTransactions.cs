using System.Data;
using Microsoft.Data.SqlClient;
using prjGoHike.Services.SpatialJoins;

static partial class CurrentDatabaseChecks
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    public static async Task RunAsync()
    {
        await ReadOnlyAsync();
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        Check(await ValueAsync<int>(connection, null, "SELECT COUNT(*) FROM dbo.BackgroundJobRuns WHERE JobType='SpatialJoin' AND Status NOT IN ('Succeeded','Failed');") == 0,
            "Do not run current DB tests alongside active spatial work.");
        var originalAssociations = await ValueAsync<string>(connection, null, "SELECT * FROM dbo.TrailIndicators ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;");
        var originalFlags = await ValueAsync<string>(connection, null, FlagsSnapshot);
        var originalFeatures = await ValueAsync<string>(connection, null, "SELECT FeatureId,Trail_Id,FeatureType,FeatureName,CONVERT(varbinary(max),Location) AS Shape,IsAvailable,ReliabilityLevel FROM dbo.TrailFeatures ORDER BY FeatureId FOR JSON PATH,INCLUDE_NULL_VALUES;");
        var originalChecks = await ValueAsync<string>(connection, null, ChecksSnapshot);
        using var stream = typeof(SpatialJoinStore).Assembly.GetManifestResourceStream("prjGoHike.Services.SpatialJoins.Synchronize.sql")!;
        using var reader = new StreamReader(stream);
        var syncSql = await reader.ReadToEndAsync();

        foreach (var scenario in new[] { "normal", "wide", "empty", "missing", "srid", "empty-shape", "unsupported", "rollback" })
        {
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                Check(await ValueAsync<int>(connection, transaction, "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource='GoHike:SpatialJoin',@LockMode='Exclusive',@LockOwner='Transaction',@DbPrincipal='public',@LockTimeout=5000; SELECT @r;") >= 0,
                    "Current database transaction owns the business lock.");
                var fixture = await SeedCurrentAsync(connection, transaction);
                var protectedBefore = await ValueAsync<string>(connection, transaction, "SELECT * FROM dbo.TrailIndicators WHERE EvaluatedScore IS NOT NULL ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;");
                var threshold = scenario == "wide" ? 10000.00m : 100m;
                await ExecuteAsync(connection, transaction, "UPDATE dbo.BackgroundJobRuns SET DistanceMeters=@DistanceMeters WHERE Id=@RunId;",
                    new SqlParameter("@RunId", SqlDbType.BigInt) { Value = fixture.RunId },
                    new SqlParameter("@DistanceMeters", SqlDbType.Decimal) { Precision = 12, Scale = 2, Value = threshold });
                var expectedError = scenario switch { "missing" => 51001, "srid" or "empty-shape" or "unsupported" => 51002, "rollback" => 51199, _ => 0 };
                switch (scenario)
                {
                    case "missing":
                        await ExecuteAsync(connection, transaction, "DELETE dbo.TrailSegments WHERE Trail_Id=@Trail2;", fixture.Parameters());
                        break;
                    case "srid":
                        // Constraint changes, fixture rows and source flags are all rolled back together.
                        await ExecuteAsync(connection, transaction, "ALTER TABLE dbo.IndicatorSegments NOCHECK CONSTRAINT CK_IndicatorSegments_SRID; UPDATE dbo.IndicatorSegments SET Shape=geography::Point(24,121,4269) WHERE IndicatorSegmentId=@PointSegment;", fixture.Parameters());
                        break;
                    case "empty-shape":
                        await ExecuteAsync(connection, transaction, "UPDATE dbo.TrailSegments SET Shape=geography::STGeomFromText('LINESTRING EMPTY',4326) WHERE TrailSegment_Id=@NearSegment;", fixture.Parameters());
                        break;
                    case "unsupported":
                        await ExecuteAsync(connection, transaction, "UPDATE dbo.IndicatorSegments SET Shape=geography::STGeomFromText('GEOMETRYCOLLECTION (POINT (121 24))',4326) WHERE IndicatorSegmentId=@PointSegment;", fixture.Parameters());
                        break;
                    case "empty":
                        await ExecuteAsync(connection, transaction, "UPDATE dbo.Trails SET IsPublished=0; UPDATE dbo.Indicators SET IsActive=0;");
                        break;
                    case "rollback":
                        await ExecuteAsync(connection, transaction, "CREATE TRIGGER dbo.TestSpatialJoinRollback ON dbo.BackgroundJobRuns AFTER UPDATE AS IF EXISTS(SELECT 1 FROM inserted WHERE Status='Succeeded') THROW 51199,'Injected test failure at success update',1;");
                        break;
                }
                try
                {
                    await ExecuteAsync(connection, transaction, syncSql,
                        new("@RunId", SqlDbType.BigInt) { Value = fixture.RunId },
                        new("@JobId", SqlDbType.VarChar, 100) { Value = "current-db-rollback-test" },
                        new("@DistanceMeters", SqlDbType.Decimal) { Precision = 12, Scale = 2, Value = threshold });
                    Check(expectedError == 0, "An invalid-source scenario must fail.");
                    Check(await ValueAsync<string>(connection, transaction, "SELECT Status FROM dbo.BackgroundJobRuns WHERE Id=@RunId;", fixture.Parameters()) == "Succeeded", "Success status is in the same transaction as candidate writes.");
                    Check(await ValueAsync<string>(connection, transaction, "SELECT * FROM dbo.TrailIndicators WHERE EvaluatedScore IS NOT NULL ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;") == protectedBefore,
                        "Every scored field, including zero, remains unchanged.");
                    var candidates = await ValueAsync<int>(connection, transaction, "SELECT COUNT(*) FROM dbo.TrailIndicators WHERE EvaluatedScore IS NULL;");
                    Check(candidates == (scenario == "empty" ? 0 : scenario == "wide" ? 3 : 2), "Correct candidate count after MIN distance, cutoff and stale-row deletion.");
                    if (scenario != "empty")
                    {
                        Check(await ValueAsync<int>(connection, transaction, "SELECT COUNT(*) FROM dbo.TrailIndicators WHERE Trail_Id=@Trail1 AND IndicatorId IN (@Indicator1,@Indicator2) AND DistanceMeters=0 AND RawScore IS NULL AND OverlapRatio IS NULL AND EvaluatedScore IS NULL;", fixture.Parameters()) == 2,
                            "Point/line and polygon intersection candidates have zero stored distance and unknown scores.");
                        Check(await ValueAsync<decimal>(connection, transaction, "SELECT IndicatorWeightSnapshot FROM dbo.TrailIndicators WHERE Trail_Id=@Trail1 AND IndicatorId=@Indicator1;", fixture.Parameters()) == 1.234m,
                            "Weight is copied from the current indicator, not old candidate/client input.");
                        var boundary = await ValueAsync<double>(connection, transaction, "SELECT i.Shape.STDistance(t.Shape) FROM dbo.IndicatorSegments i CROSS JOIN dbo.TrailSegments t WHERE i.IndicatorId=@Indicator3 AND t.TrailSegment_Id=@NearSegment;", fixture.Parameters());
                        Check(boundary > 100 && boundary < 100.005, "Original 100.004m distance is excluded at 100m despite decimal rounding.");
                    }
                }
                catch (SqlException ex) when (expectedError != 0 && ex.Number == expectedError)
                {
                    Check(true, "Expected source/transaction validation failure propagated.");
                }
            }
            finally
            {
                if (transaction.Connection is not null)
                {
                    try { await transaction.RollbackAsync(); }
                    catch (InvalidOperationException) when (transaction.Connection is null) { }
                }
            }
            Check(await ValueAsync<string>(connection, null, "SELECT * FROM dbo.TrailIndicators ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;") == originalAssociations,
                "Rollback restores all original associations.");
            Check(await ValueAsync<string>(connection, null, FlagsSnapshot) == originalFlags, "Rollback restores every original source flag and removes fixtures.");
            Check(await ValueAsync<string>(connection, null, ChecksSnapshot) == originalChecks, "Rollback restores original constraint trust/enabled states.");
            Check(await ValueAsync<string>(connection, null, "SELECT FeatureId,Trail_Id,FeatureType,FeatureName,CONVERT(varbinary(max),Location) AS Shape,IsAvailable,ReliabilityLevel FROM dbo.TrailFeatures ORDER BY FeatureId FOR JSON PATH,INCLUDE_NULL_VALUES;") == originalFeatures,
                "TrailFeatures is unchanged.");
            Console.WriteLine($"PASS current SQL rollback scenario: {scenario}");
        }
        await VerifyCurrentStoreAndHangfireAsync(connection);
        Console.WriteLine($"PASS: {_checks} current database SQL/Hangfire checks.");
    }

    private static async Task<Fixture> SeedCurrentAsync(SqlConnection connection, SqlTransaction transaction)
    {
        await using var command = new SqlCommand(CurrentSeed, connection, transaction) { CommandTimeout = 30 };
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4),
            reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7), reader.GetInt64(8), reader.GetInt64(9));
    }

    private static async Task ExecuteAsync(SqlConnection connection, SqlTransaction? transaction, string sql, params SqlParameter[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 300 };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ValueAsync<T>(SqlConnection connection, SqlTransaction? transaction, string sql, params SqlParameter[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 30 };
        command.Parameters.AddRange(parameters);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private sealed record Fixture(long Trail1, long Trail2, long Trail3, long Indicator1, long Indicator2, long Indicator3,
        long Indicator4, long NearSegment, long PointSegment, long RunId)
    {
        public SqlParameter[] Parameters() =>
        [
            new("@Trail1",SqlDbType.BigInt){Value=Trail1}, new("@Trail2",SqlDbType.BigInt){Value=Trail2}, new("@Trail3",SqlDbType.BigInt){Value=Trail3},
            new("@Indicator1",SqlDbType.BigInt){Value=Indicator1}, new("@Indicator2",SqlDbType.BigInt){Value=Indicator2},
            new("@Indicator3",SqlDbType.BigInt){Value=Indicator3},new("@Indicator4",SqlDbType.BigInt){Value=Indicator4},
            new("@NearSegment",SqlDbType.BigInt){Value=NearSegment},new("@PointSegment",SqlDbType.BigInt){Value=PointSegment},new("@RunId",SqlDbType.BigInt){Value=RunId}
        ];
    }

    private const string FlagsSnapshot = """
        SELECT (SELECT Trail_Id,IsPublished FROM dbo.Trails ORDER BY Trail_Id FOR JSON PATH) + N'|' +
            (SELECT IndicatorId,IsActive FROM dbo.Indicators ORDER BY IndicatorId FOR JSON PATH);
        """;
    private const string ChecksSnapshot = "SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints ORDER BY name FOR JSON PATH;";

    private const string CurrentSeed = """
        SET NOCOUNT ON;
        UPDATE dbo.Trails SET IsPublished=0;
        UPDATE dbo.Indicators SET IsActive=0;
        DECLARE @t1 bigint,@t2 bigint,@t3 bigint,@i1 bigint,@i2 bigint,@i3 bigint,@i4 bigint,@near bigint,@point bigint,@run bigint;
        INSERT dbo.Trails (Trail_Name,Region,Difficulty_Level,Permit_Required,Guide_Required,IsPublished) VALUES(N'__spatial_test_near',N'test',1,0,0,1); SET @t1=SCOPE_IDENTITY();
        INSERT dbo.Trails (Trail_Name,Region,Difficulty_Level,Permit_Required,Guide_Required,IsPublished) VALUES(N'__spatial_test_far',N'test',1,0,0,1); SET @t2=SCOPE_IDENTITY();
        INSERT dbo.Trails (Trail_Name,Region,Difficulty_Level,Permit_Required,Guide_Required,IsPublished) VALUES(N'__spatial_test_hidden',N'test',1,0,0,0); SET @t3=SCOPE_IDENTITY();
        INSERT dbo.Indicators (IndicatorName,IndicatorType,Weight,IsActive) VALUES(N'__spatial_test_point','Test',1.234,1); SET @i1=SCOPE_IDENTITY();
        INSERT dbo.Indicators (IndicatorName,IndicatorType,Weight,IsActive) VALUES(N'__spatial_test_polygon','Test',2,1); SET @i2=SCOPE_IDENTITY();
        INSERT dbo.Indicators (IndicatorName,IndicatorType,Weight,IsActive) VALUES(N'__spatial_test_boundary','Test',3,1); SET @i3=SCOPE_IDENTITY();
        INSERT dbo.Indicators (IndicatorName,IndicatorType,Weight,IsActive) VALUES(N'__spatial_test_inactive','Test',4,0); SET @i4=SCOPE_IDENTITY();
        INSERT dbo.TrailSegments (Trail_Id,Shape) VALUES(@t1,geography::STGeomFromText('LINESTRING (123 24,123.001 24)',4326));
        INSERT dbo.TrailSegments (Trail_Id,Shape) VALUES(@t1,geography::STGeomFromText('LINESTRING (121 24,121.001 24)',4326)); SET @near=SCOPE_IDENTITY();
        INSERT dbo.TrailSegments (Trail_Id,Shape) VALUES(@t2,geography::STGeomFromText('LINESTRING (120 24,120.001 24)',4326));
        INSERT dbo.IndicatorSegments (IndicatorId,Shape) VALUES(@i1,geography::Point(24,121.0005,4326)); SET @point=SCOPE_IDENTITY();
        INSERT dbo.IndicatorSegments (IndicatorId,Shape) VALUES(@i1,geography::Point(24,124,4326));
        INSERT dbo.IndicatorSegments (IndicatorId,Shape) VALUES(@i2,geography::STGeomFromText('POLYGON ((121.0004 23.9999,121.0007 23.9999,121.0007 24.0001,121.0004 24.0001,121.0004 23.9999))',4326));
        DECLARE @lo float=121.001,@hi float=121.003,@mid float,@n int=0,@line geography=geography::STGeomFromText('LINESTRING (121 24,121.001 24)',4326);
        WHILE @n<60 BEGIN
            SET @mid=(@lo+@hi)/2;
            IF geography::Point(24,@mid,4326).STDistance(@line)<100.004 SET @lo=@mid; ELSE SET @hi=@mid;
            SET @n=@n+1;
        END;
        INSERT dbo.IndicatorSegments (IndicatorId,Shape) VALUES(@i3,geography::Point(24,@mid,4326));
        INSERT dbo.TrailIndicators (Trail_Id,IndicatorId,DistanceMeters,IndicatorWeightSnapshot,OverlapRatio,RawScore,EvaluatedScore,EvaluatedAt) VALUES
            (@t1,@i1,99,9,0.5,7,NULL,'2010-01-01'),(@t2,@i3,999,9,NULL,NULL,NULL,'2010-01-01'),
            (@t2,@i1,321,8,0.42,0,0,'2010-01-01'),(@t1,@i4,555,8,0.1,9,9,'2010-01-01'),(@t3,@i1,666,8,NULL,3,3,'2010-01-01');
        INSERT dbo.BackgroundJobRuns (JobType,Status,HangfireJobId,DistanceMeters,StartedAt)
            VALUES('SpatialJoin','Processing','current-db-rollback-test',100,SYSUTCDATETIME()); SET @run=SCOPE_IDENTITY();
        SELECT @t1,@t2,@t3,@i1,@i2,@i3,@i4,@near,@point,@run;
        """;
}
