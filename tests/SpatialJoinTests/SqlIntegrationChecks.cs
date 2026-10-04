using System.Data;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using prjGoHike.Models;
using prjGoHike.Services.SpatialJoins;

static class SqlIntegrationChecks
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        var connectionString = Environment.GetEnvironmentVariable("GOHIKE_SPATIAL_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("SKIP: SQL Server/Hangfire integration requires GOHIKE_SPATIAL_TEST_CONNECTION pointing to an EMPTY dedicated GoHikeSpatialJoinTests* database.");
            return;
        }
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!builder.InitialCatalog.StartsWith("GoHikeSpatialJoinTests", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Integration tests require an empty database named GoHikeSpatialJoinTests*; never use the application database.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        if (await ScalarAsync<int>(connection, "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0;") != 0)
            throw new InvalidOperationException("The integration database must be empty. Tests leave their fixtures for inspection; use a new database for each run.");
        await ExecuteAsync(connection, Schema);
        await ExecuteAsync(connection, Seed);
        await using var context = new SqlTestContext(connectionString);
        var options = Options.Create(new SpatialJoinOptions { Enabled = true, LockTimeoutMilliseconds = 300 });
        var store = new SpatialJoinStore(context, options);
        var job = new SpatialJoinJob(store, NullLogger<SpatialJoinJob>.Instance);
        var protectedBefore = await SnapshotAsync(connection, "WHERE EvaluatedScore IS NOT NULL");
        var featureBefore = await ScalarAsync<string>(connection, "SELECT Trail_Id, FeatureName, Location.STAsText() AS Shape, IsAvailable FROM dbo.TrailFeatures FOR JSON PATH;");
        var rawDistance = await ScalarAsync<double>(connection, "SELECT i.Shape.STDistance(t.Shape) FROM dbo.IndicatorSegments i CROSS JOIN dbo.TrailSegments t WHERE i.IndicatorId = 3 AND t.TrailSegment_Id = 2;");
        check(rawDistance > 100 && rawDistance < 100.005, "Fixture exercises original 100.004m cutoff before decimal rounding.");

        async Task<BackgroundJobRun> RunAsync(decimal distance, string jobId)
        {
            var created = await store.CreateAsync(distance, CancellationToken.None);
            check(created.Created, "New Run can start after terminal predecessor.");
            await job.ExecuteRunAsync(created.Run.Id, jobId, CancellationToken.None);
            return (await store.GetAsync(created.Run.Id, CancellationToken.None))!;
        }
        var first = await RunAsync(100m, "sql-first");
        check(first.Status == "Succeeded" && first.FinishedAt?.Kind == DateTimeKind.Utc, "SQL success and result synchronization commit together.");
        check(await ScalarAsync<int>(connection, "SELECT COUNT(*) FROM dbo.TrailIndicators WHERE EvaluatedScore IS NULL;") == 2, "Multiple segments produce one candidate per pair, boundary candidate is excluded.");
        check(await ScalarAsync<int>(connection, "SELECT COUNT(*) FROM dbo.TrailIndicators WHERE Trail_Id=1 AND IndicatorId IN (1,2) AND DistanceMeters=0 AND EvaluatedScore IS NULL AND RawScore IS NULL AND OverlapRatio IS NULL;") == 2,
            "Point on line and intersecting polygon have zero distance and unknown scores/overlap.");
        check(await ScalarAsync<decimal>(connection, "SELECT IndicatorWeightSnapshot FROM dbo.TrailIndicators WHERE Trail_Id=1 AND IndicatorId=1;") == 1.234m,
            "Unscored update takes current server weight.");
        check(await SnapshotAsync(connection, "WHERE EvaluatedScore IS NOT NULL") == protectedBefore,
            "Nonzero and zero scores retain every column, including inactive/unpublished sources.");
        var successfulSnapshot = await SnapshotAsync(connection, "");
        await Task.WhenAll(job.ExecuteRunAsync(first.Id, "sql-first", CancellationToken.None), job.ExecuteRunAsync(first.Id, "sql-first", CancellationToken.None));
        check(await SnapshotAsync(connection, "") == successfulSnapshot, "Committed Run cannot rewrite results on duplicate execution.");

        // Real application-lock contention and concurrent API admission.
        await using var secondContext = new SqlTestContext(connectionString);
        var secondStore = new SpatialJoinStore(secondContext, options);
        var admitted = await Task.WhenAll(store.CreateAsync(50m, CancellationToken.None), secondStore.CreateAsync(75m, CancellationToken.None));
        check(admitted.Count(x => x.Created) == 1 && admitted[0].Run.Id == admitted[1].Run.Id, "Concurrent creations admit one nonterminal Run.");
        await store.BindAsync(admitted[0].Run.Id, "sql-concurrent", false, CancellationToken.None);
        await store.ReconcileAsync(admitted[0].Run.Id, "sql-concurrent", () => "Failed", CancellationToken.None);
        await using (var transaction = (SqlTransaction)await connection.BeginTransactionAsync())
        {
            await using var lockCommand = new SqlCommand("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource='GoHike:SpatialJoin', @LockMode='Exclusive', @LockOwner='Transaction', @DbPrincipal='public'; SELECT @r;", connection, transaction);
            check(Convert.ToInt32(await lockCommand.ExecuteScalarAsync()) >= 0, "Test owns business application lock.");
            try { await store.CreateAsync(100m, CancellationToken.None); check(false, "Locked creation must fail."); }
            catch (SpatialJoinLockException ex) { check(ex.ReturnCode < 0, "Lock timeout is checked and cannot proceed."); }
            await transaction.RollbackAsync();
        }

        async Task ExpectSourceFailureAsync(string mutation, string restoration, int error)
        {
            var before = await SnapshotAsync(connection, "");
            await ExecuteAsync(connection, mutation);
            var run = (await store.CreateAsync(100m, CancellationToken.None)).Run;
            var executor = "sql-invalid-" + run.Id;
            try { await job.ExecuteRunAsync(run.Id, executor, CancellationToken.None); check(false, "Invalid source must fail."); }
            catch (SqlException ex) { check(ex.Number == error, "Source validation reports expected controlled error."); }
            var failedAttempt = (await store.GetAsync(run.Id, CancellationToken.None))!;
            check(failedAttempt.Status == "Processing" && failedAttempt.FinishedAt is null, "Attempt validation failure is retryable, not final.");
            check(await SnapshotAsync(connection, "") == before, "Invalid source leaves every association unchanged.");
            await store.ReconcileAsync(run.Id, executor, () => "Failed", CancellationToken.None);
            await ExecuteAsync(connection, restoration);
        }
        await ExpectSourceFailureAsync("INSERT dbo.Trails VALUES (99,N'missing',1);", "DELETE dbo.Trails WHERE Trail_Id=99;", 51001);
        await ExpectSourceFailureAsync("UPDATE dbo.IndicatorSegments SET Shape=geography::Point(24,121,4269) WHERE IndicatorSegmentId=1;",
            "UPDATE dbo.IndicatorSegments SET Shape=geography::Point(24,121.0005,4326) WHERE IndicatorSegmentId=1;", 51002);
        await ExpectSourceFailureAsync("UPDATE dbo.TrailSegments SET Shape=geography::STGeomFromText('LINESTRING EMPTY',4326) WHERE TrailSegment_Id=2;",
            "UPDATE dbo.TrailSegments SET Shape=geography::STGeomFromText('LINESTRING (121 24,121.001 24)',4326) WHERE TrailSegment_Id=2;", 51002);
        await ExpectSourceFailureAsync("UPDATE dbo.IndicatorSegments SET Shape=geography::STGeomFromText('GEOMETRYCOLLECTION (POINT (121 24))',4326) WHERE IndicatorSegmentId=1;",
            "UPDATE dbo.IndicatorSegments SET Shape=geography::Point(24,121.0005,4326) WHERE IndicatorSegmentId=1;", 51002);

        // Fail at the final success update, after all candidate writes have executed.
        await ExecuteAsync(connection, "CREATE TRIGGER dbo.TestRejectSuccess ON dbo.BackgroundJobRuns AFTER UPDATE AS IF EXISTS (SELECT 1 FROM inserted WHERE Status='Succeeded') THROW 51199, 'Test final commit failure', 1;");
        var beforeRollback = await SnapshotAsync(connection, "");
        var rollbackRun = (await store.CreateAsync(100m, CancellationToken.None)).Run;
        try { await job.ExecuteRunAsync(rollbackRun.Id, "sql-rollback", CancellationToken.None); check(false, "Injected success-update failure must propagate."); }
        catch (SqlException ex) { check(ex.Number == 51199, "Injected transaction failure reached the final state update."); }
        check(await SnapshotAsync(connection, "") == beforeRollback && (await store.GetAsync(rollbackRun.Id, CancellationToken.None))!.Status != "Succeeded",
            "Candidate writes and Succeeded roll back together.");
        await ExecuteAsync(connection, "DROP TRIGGER dbo.TestRejectSuccess;");
        await job.ExecuteRunAsync(rollbackRun.Id, "sql-rollback", CancellationToken.None);
        check((await store.GetAsync(rollbackRun.Id, CancellationToken.None))!.Status == "Succeeded", "Rolled-back attempt can recover and commit.");

        await ExecuteAsync(connection, "UPDATE dbo.Indicators SET IsActive=0 WHERE IndicatorId=2;");
        await RunAsync(100m, "sql-disable");
        check(await ScalarAsync<int>(connection, "SELECT COUNT(*) FROM dbo.TrailIndicators WHERE Trail_Id=1 AND IndicatorId=2;") == 0, "Disabled indicator removes its unscored candidate.");
        await ExecuteAsync(connection, "UPDATE dbo.Trails SET IsPublished=0; UPDATE dbo.Indicators SET IsActive=0;");
        await RunAsync(100m, "sql-empty");
        check(await ScalarAsync<int>(connection, "SELECT COUNT(*) FROM dbo.TrailIndicators WHERE EvaluatedScore IS NULL;") == 0, "Empty active sources are a valid empty result.");
        check(await SnapshotAsync(connection, "WHERE EvaluatedScore IS NOT NULL") == protectedBefore, "Empty results still preserve scored rows.");
        check(await ScalarAsync<string>(connection, "SELECT Trail_Id, FeatureName, Location.STAsText() AS Shape, IsAvailable FROM dbo.TrailFeatures FOR JSON PATH;") == featureBefore,
            "TrailFeatures is untouched by every sync.");
        await ExecuteAsync(connection, "UPDATE dbo.Trails SET IsPublished=1 WHERE Trail_Id IN (1,2); UPDATE dbo.Indicators SET IsActive=1 WHERE IndicatorId IN (1,2,3);");
        await VerifyHangfireAsync(connectionString, connection, check);
        Console.WriteLine("PASS: live SQL Server geography, application locks, rollback, protected scores and Hangfire execution/retry/recovery checks. Test fixtures remain in the dedicated database.");
    }

    private static async Task VerifyHangfireAsync(string connectionString, SqlConnection connection, Action<bool, string> check)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hangfire:Enabled"] = "true", ["Hangfire:PrepareSchemaIfNecessary"] = "true",
            ["ConnectionStrings:Hangfire"] = connectionString
        });
        builder.Services.AddScoped<GoHikeDataContext>(_ => new SqlTestContext(connectionString));
        builder.Services.AddSpatialJoins(builder.Configuration);
        // Only tests accelerate retries. Production retains Hangfire's default increasing delays.
        builder.Services.AddHangfire((_, config) => config.UseFilter(new ImmediateRetryFilter()));
        using var host = builder.Build();
        await host.StartAsync();
        try
        {
            await using var scope = host.Services.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<SpatialJoinService>();
            var store = scope.ServiceProvider.GetRequiredService<ISpatialJoinStore>();
            var successful = await service.CreateAsync(100m, CancellationToken.None);
            check(successful.Created, "Real Hangfire client accepts persisted Run.");
            await WaitForAsync(store, successful.Run.Id, "Succeeded");
            check((await store.GetAsync(successful.Run.Id, CancellationToken.None))!.FinishedAt is not null, "Real worker commits SQL synchronization and success.");

            await ExecuteAsync(connection, "INSERT dbo.Trails VALUES (99,N'missing',1);");
            var failing = await service.CreateAsync(100m, CancellationToken.None);
            await WaitForAsync(store, failing.Run.Id, "Failed");
            var failed = (await store.GetAsync(failing.Run.Id, CancellationToken.None))!;
            using (var jobs = scope.ServiceProvider.GetRequiredService<JobStorage>().GetConnection())
            {
                check(jobs.GetStateData(failed.HangfireJobId!)?.Name == "Failed", "Reconciliation observes applied final Hangfire failure.");
                check(jobs.GetJobParameter(failed.HangfireJobId!, "RetryCount") == "3", "Three actual Hangfire retries are exhausted.");
            }
            check(failed.FinishedAt is not null && failed.ErrorMessage?.Contains("缺少空間") == true, "Failure API data is terminal and controlled.");
            await ExecuteAsync(connection, "DELETE dbo.Trails WHERE Trail_Id=99;");

            // Pending survives client outage and can later be recovered by the production recovery service.
            var unavailableService = new SpatialJoinService(store, Options.Create(new SpatialJoinOptions { Enabled = true }),
                NullLogger<SpatialJoinService>.Instance, new UnavailableClient(), scope.ServiceProvider.GetRequiredService<JobStorage>());
            var pending = await unavailableService.CreateAsync(50m, CancellationToken.None);
            check(pending.Run.Status == "Pending", "Real business DB persists request during enqueue outage.");
            await ExecuteAsync(connection, "UPDATE dbo.BackgroundJobRuns SET CreatedAt=DATEADD(minute,-2,SYSUTCDATETIME()) WHERE Id=@Id;",
                new SqlParameter("@Id", SqlDbType.BigInt) { Value = pending.Run.Id });
            await service.RecoverAsync(CancellationToken.None);
            await WaitForAsync(store, pending.Run.Id, "Succeeded");
            check(true, "Production recovery queues and completes stranded Pending request.");
        }
        finally { await host.StopAsync(); }
    }

    private static async Task WaitForAsync(ISpatialJoinStore store, long id, string status)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        while (true)
        {
            var run = await store.GetAsync(id, timeout.Token);
            if (run?.Status == status) return;
            if (run is not null && SpatialJoinRules.IsTerminal(run.Status)) throw new InvalidOperationException($"Expected {status}, got {run.Status} for test Run {id}.");
            await Task.Delay(100, timeout.Token);
        }
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql, params SqlParameter[] parameters)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }
    private static async Task<T> ScalarAsync<T>(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        return (T)(await command.ExecuteScalarAsync())!;
    }
    private static Task<string> SnapshotAsync(SqlConnection connection, string predicate) => ScalarAsync<string>(connection,
        "SELECT * FROM dbo.TrailIndicators " + predicate + " ORDER BY Trail_Id,IndicatorId FOR JSON PATH, INCLUDE_NULL_VALUES;");

    private const string Schema = """
        CREATE TABLE dbo.Trails (Trail_Id bigint PRIMARY KEY, Trail_Name nvarchar(120) NOT NULL, IsPublished bit NOT NULL);
        CREATE TABLE dbo.Indicators (IndicatorId bigint PRIMARY KEY, IndicatorName nvarchar(120) NOT NULL, Weight decimal(6,3) NOT NULL CHECK (Weight>=0), IsActive bit NOT NULL);
        CREATE TABLE dbo.TrailSegments (TrailSegment_Id bigint PRIMARY KEY, Trail_Id bigint NOT NULL REFERENCES dbo.Trails(Trail_Id), Shape geography NOT NULL);
        CREATE TABLE dbo.IndicatorSegments (IndicatorSegmentId bigint PRIMARY KEY, IndicatorId bigint NOT NULL REFERENCES dbo.Indicators(IndicatorId), Shape geography NOT NULL);
        CREATE TABLE dbo.TrailIndicators (
            Trail_Id bigint NOT NULL REFERENCES dbo.Trails(Trail_Id), IndicatorId bigint NOT NULL REFERENCES dbo.Indicators(IndicatorId),
            DistanceMeters decimal(12,2) NULL CHECK (DistanceMeters>=0), IndicatorWeightSnapshot decimal(6,3) NOT NULL CHECK (IndicatorWeightSnapshot>=0),
            OverlapRatio decimal(7,6) NULL CHECK (OverlapRatio>=0 AND OverlapRatio<=1), RawScore decimal(10,4) NULL, EvaluatedScore decimal(10,4) NULL,
            EvaluatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), PRIMARY KEY (Trail_Id,IndicatorId));
        CREATE TABLE dbo.TrailFeatures (FeatureId bigint PRIMARY KEY, Trail_Id bigint NOT NULL REFERENCES dbo.Trails(Trail_Id),
            FeatureName nvarchar(120) NOT NULL, Location geography NOT NULL, IsAvailable bit NOT NULL);
        CREATE TABLE dbo.BackgroundJobRuns (Id bigint IDENTITY PRIMARY KEY, JobType varchar(50) NOT NULL,
            Status varchar(20) NOT NULL CHECK (Status IN ('Pending','Queued','Processing','RetryPending','Succeeded','Failed')),
            HangfireJobId varchar(100) NULL, DistanceMeters decimal(12,2) NOT NULL CHECK (DistanceMeters>=0.01 AND DistanceMeters<=10000.00),
            CreatedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(), StartedAt datetime2(0) NULL, FinishedAt datetime2(0) NULL, ErrorMessage nvarchar(500) NULL);
        CREATE INDEX IX_BackgroundJobRuns_JobType_Status_CreatedAt ON dbo.BackgroundJobRuns(JobType,Status,CreatedAt);
        """;

    private const string Seed = """
        INSERT dbo.Trails VALUES (1,N'near',1),(2,N'far',1),(3,N'unpublished',0);
        INSERT dbo.Indicators VALUES (1,N'point',1.234,1),(2,N'polygon',2,1),(3,N'boundary',3,1),(4,N'disabled',4,0);
        INSERT dbo.TrailSegments VALUES
            (1,1,geography::STGeomFromText('LINESTRING (123 24,123.001 24)',4326)),
            (2,1,geography::STGeomFromText('LINESTRING (121 24,121.001 24)',4326)),
            (3,2,geography::STGeomFromText('LINESTRING (120 24,120.001 24)',4326));
        INSERT dbo.IndicatorSegments VALUES
            (1,1,geography::Point(24,121.0005,4326)),(2,1,geography::Point(24,124,4326)),
            (3,2,geography::STGeomFromText('POLYGON ((121.0004 23.9999,121.0007 23.9999,121.0007 24.0001,121.0004 24.0001,121.0004 23.9999))',4326)),
            (4,3,geography::Point(24,121.002,4326));
        DECLARE @lo float=121.001, @hi float=121.003, @mid float, @n int=0,
            @line geography=geography::STGeomFromText('LINESTRING (121 24,121.001 24)',4326);
        WHILE @n<60 BEGIN
            SET @mid=(@lo+@hi)/2;
            IF geography::Point(24,@mid,4326).STDistance(@line)<100.004 SET @lo=@mid; ELSE SET @hi=@mid;
            SET @n=@n+1;
        END;
        UPDATE dbo.IndicatorSegments SET Shape=geography::Point(24,@mid,4326) WHERE IndicatorId=3;
        INSERT dbo.TrailIndicators VALUES
            (1,1,99,9,0.5,7,NULL,'2010-01-01'),(2,3,999,9,NULL,NULL,NULL,'2010-01-01'),
            (2,1,321,8,0.42,0,0,'2010-01-01'),(1,4,555,8,0.1,9,9,'2010-01-01'),(3,1,666,8,NULL,3,3,'2010-01-01');
        INSERT dbo.TrailFeatures VALUES (1,1,N'untouched',geography::Point(24,121,4326),1);
        """;
}

sealed class SqlTestContext(string connectionString) : GoHikeDataContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder) => builder.UseSqlServer(connectionString, o => o.UseNetTopologySuite());
}

sealed class ImmediateRetryFilter : JobFilterAttribute, IElectStateFilter
{
    public ImmediateRetryFilter() { Order = 1000; }
    public void OnStateElection(ElectStateContext context)
    {
        if (context.BackgroundJob.Job.Type == typeof(SpatialJoinJob) && context.CandidateState is ScheduledState)
            context.CandidateState = new EnqueuedState();
    }
}

sealed class UnavailableClient : IBackgroundJobClient
{
    public string Create(Job job, IState state) => throw new TimeoutException("Simulated enqueue outage");
    public bool ChangeState(string jobId, IState state, string expectedState) => throw new NotSupportedException();
}
