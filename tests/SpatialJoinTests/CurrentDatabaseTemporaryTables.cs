using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using prjGoHike.Services.SpatialJoins;

static partial class CurrentDatabaseChecks
{
    public static async Task TemporaryTablesAsync()
    {
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        var original = await ValueAsync<string>(connection, null, "SELECT * FROM dbo.TrailIndicators ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;");
        var flags = await ValueAsync<string>(connection, null, FlagsSnapshot);
        using var stream = typeof(SpatialJoinStore).Assembly.GetManifestResourceStream("prjGoHike.Services.SpatialJoins.Synchronize.sql")!;
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync();
        static string Temporary(string text)
        {
            foreach (var table in new[] { "BackgroundJobRuns", "TrailIndicators", "TrailSegments", "IndicatorSegments", "Trails", "Indicators" })
                text = text.Replace("dbo." + table, "#" + table, StringComparison.Ordinal);
            return text;
        }
        sql = Temporary(sql);
        foreach (var scenario in new[] { "normal", "wide", "empty", "missing", "srid", "empty-shape", "unsupported", "rollback" })
        {
            // Only session-local tempdb tables are created or changed. No application DML/DDL or Hangfire jobs.
            await ExecuteAsync(connection, null, """
                SELECT TOP (0) * INTO #Trails FROM dbo.Trails;
                SELECT TOP (0) * INTO #Indicators FROM dbo.Indicators;
                SELECT TOP (0) * INTO #TrailSegments FROM dbo.TrailSegments;
                SELECT TOP (0) * INTO #IndicatorSegments FROM dbo.IndicatorSegments;
                SELECT TOP (0) * INTO #TrailIndicators FROM dbo.TrailIndicators;
                SELECT TOP (0) * INTO #BackgroundJobRuns FROM dbo.BackgroundJobRuns;
                ALTER TABLE #BackgroundJobRuns ADD DEFAULT SYSUTCDATETIME() FOR CreatedAt;
                """);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await using var seed = new SqlCommand(Temporary(CurrentSeed), connection, transaction) { CommandTimeout = 30 };
                Fixture fixture;
                await using (var seeded = await seed.ExecuteReaderAsync())
                {
                    await seeded.ReadAsync();
                    fixture = new(seeded.GetInt64(0),seeded.GetInt64(1),seeded.GetInt64(2),seeded.GetInt64(3),seeded.GetInt64(4),
                        seeded.GetInt64(5),seeded.GetInt64(6),seeded.GetInt64(7),seeded.GetInt64(8),seeded.GetInt64(9));
                }
                var scored = await ValueAsync<string>(connection, transaction, "SELECT * FROM #TrailIndicators WHERE EvaluatedScore IS NOT NULL ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;");
                var threshold = scenario == "wide" ? 10000.00m : 100m;
                await ExecuteAsync(connection, transaction, "UPDATE #BackgroundJobRuns SET DistanceMeters=@DistanceMeters WHERE Id=@RunId;",
                    new("@DistanceMeters",SqlDbType.Decimal){Precision=12,Scale=2,Value=threshold},new("@RunId",SqlDbType.BigInt){Value=fixture.RunId});
                var expectedError = scenario switch { "missing" => 51001, "srid" or "empty-shape" or "unsupported" => 51002, "rollback" => 51199, _ => 0 };
                switch (scenario)
                {
                    case "missing": await ExecuteAsync(connection,transaction,"DELETE #TrailSegments WHERE Trail_Id=@Trail2;",fixture.Parameters()); break;
                    case "srid": await ExecuteAsync(connection,transaction,"UPDATE #IndicatorSegments SET Shape=geography::Point(24,121,4269) WHERE IndicatorSegmentId=@PointSegment;",fixture.Parameters()); break;
                    case "empty-shape": await ExecuteAsync(connection,transaction,"UPDATE #TrailSegments SET Shape=geography::STGeomFromText('LINESTRING EMPTY',4326) WHERE TrailSegment_Id=@NearSegment;",fixture.Parameters()); break;
                    case "unsupported": await ExecuteAsync(connection,transaction,"UPDATE #IndicatorSegments SET Shape=geography::STGeomFromText('GEOMETRYCOLLECTION (POINT (121 24))',4326) WHERE IndicatorSegmentId=@PointSegment;",fixture.Parameters()); break;
                    case "empty": await ExecuteAsync(connection,transaction,"UPDATE #Trails SET IsPublished=0; UPDATE #Indicators SET IsActive=0;"); break;
                }
                var commandSql = scenario == "rollback" ? sql.Replace("UPDATE #BackgroundJobRuns", "THROW 51199,'Injected failure before success',1;\nUPDATE #BackgroundJobRuns", StringComparison.Ordinal) : sql;
                try
                {
                    await ExecuteAsync(connection,transaction,commandSql,new("@RunId",SqlDbType.BigInt){Value=fixture.RunId},
                        new("@JobId",SqlDbType.VarChar,100){Value="current-db-rollback-test"},new("@DistanceMeters",SqlDbType.Decimal){Precision=12,Scale=2,Value=threshold});
                    Check(expectedError==0,"An invalid temp source must fail.");
                    Check(await ValueAsync<int>(connection,transaction,"SELECT COUNT(*) FROM #TrailIndicators WHERE EvaluatedScore IS NULL;") == (scenario=="empty"?0:scenario=="wide"?3:2),"Correct MIN distance/cutoff candidate count.");
                    Check(await ValueAsync<string>(connection,transaction,"SELECT * FROM #TrailIndicators WHERE EvaluatedScore IS NOT NULL ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;")==scored,"All scored fields including zero remain unchanged.");
                    Check(await ValueAsync<string>(connection,transaction,"SELECT Status FROM #BackgroundJobRuns WHERE Id=@RunId;",fixture.Parameters())=="Succeeded","Success status accompanies candidate writes.");
                    if(scenario!="empty")
                    {
                        Check(await ValueAsync<int>(connection,transaction,"SELECT COUNT(*) FROM #TrailIndicators WHERE Trail_Id=@Trail1 AND IndicatorId IN (@Indicator1,@Indicator2) AND DistanceMeters=0 AND RawScore IS NULL AND OverlapRatio IS NULL AND EvaluatedScore IS NULL;",fixture.Parameters())==2,"Intersections have zero stored distance and unknown scores.");
                        Check(await ValueAsync<decimal>(connection,transaction,"SELECT IndicatorWeightSnapshot FROM #TrailIndicators WHERE Trail_Id=@Trail1 AND IndicatorId=@Indicator1;",fixture.Parameters())==1.234m,"Use current server weight.");
                        var distance=await ValueAsync<double>(connection,transaction,"SELECT i.Shape.STDistance(t.Shape) FROM #IndicatorSegments i CROSS JOIN #TrailSegments t WHERE i.IndicatorId=@Indicator3 AND t.TrailSegment_Id=@NearSegment;",fixture.Parameters());
                        Check(distance>100 && distance<100.005,"Original 100.004m cutoff is exercised before rounding.");
                    }
                }
                catch(SqlException ex) when(expectedError!=0 && ex.Number==expectedError) { Check(true,"Expected validation/atomic rollback failure propagated."); }
            }
            finally
            {
                if(transaction.Connection is not null)
                {
                    try { await transaction.RollbackAsync(); }
                    catch(InvalidOperationException) when(transaction.Connection is null) { }
                }
            }
            Check(await ValueAsync<int>(connection,null,"SELECT COUNT(*) FROM #TrailIndicators;")==0,"Rollback removes every temporary association write.");
            await ExecuteAsync(connection,null,"DROP TABLE #TrailIndicators,#TrailSegments,#IndicatorSegments,#Trails,#Indicators,#BackgroundJobRuns;");
            Console.WriteLine($"PASS current SQL temp-table scenario: {scenario}");
        }
        Check(await ValueAsync<string>(connection,null,"SELECT * FROM dbo.TrailIndicators ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;")==original,"Business associations are unchanged.");
        Check(await ValueAsync<string>(connection,null,FlagsSnapshot)==flags,"Business source flags are unchanged.");
        await using var context=new SqlTestContext(ConnectionString());
        var store=new SpatialJoinStore(context,Options.Create(new SpatialJoinOptions()));
        await store.GetAssociationsAsync(new(),CancellationToken.None);
        Check(true,"Actual read-only candidate query compiles against the current schema.");
        Console.WriteLine($"PASS: {_checks} current SQL engine checks using only temporary tables. Business data/schema untouched; Hangfire integration remains pending.");
    }
}
