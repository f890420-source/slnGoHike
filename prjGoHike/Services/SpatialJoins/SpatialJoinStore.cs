using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;

namespace prjGoHike.Services.SpatialJoins;

public sealed class SpatialJoinStore(GoHikeDataContext context, IOptions<SpatialJoinOptions> options) : ISpatialJoinStore
{
    private const string Columns = "Id, JobType, Status, HangfireJobId, DistanceMeters, CreatedAt, StartedAt, FinishedAt, ErrorMessage";
    private const string ActivePredicate = "JobType = 'SpatialJoin' AND Status IN ('Pending', 'Queued', 'Processing', 'RetryPending')";
    private static readonly string SynchronizationSql = LoadSynchronizationSql();
    private readonly SpatialJoinOptions _options = options.Value;

    public Task<SpatialJoinCreation> CreateAsync(decimal distance, CancellationToken cancellationToken)
    {
        if (!SpatialJoinRules.IsValidDistance(distance)) throw new ArgumentOutOfRangeException(nameof(distance));
        return WithLockAsync(async (connection, transaction) =>
        {
            var active = (await ReadRunsAsync(connection, transaction,
                $"SELECT TOP (1) {Columns} FROM dbo.BackgroundJobRuns WHERE {ActivePredicate} ORDER BY Id;", [], cancellationToken)).FirstOrDefault();
            if (active is not null) return new SpatialJoinCreation(false, active);
            var run = (await ReadRunsAsync(connection, transaction, $"""
                INSERT INTO dbo.BackgroundJobRuns (JobType, Status, DistanceMeters, CreatedAt)
                OUTPUT {string.Join(", ", Columns.Split(", ").Select(c => "inserted." + c))}
                VALUES ('SpatialJoin', 'Pending', @DistanceMeters, SYSUTCDATETIME());
                """, [DistanceParameter(distance)], cancellationToken)).Single();
            return new SpatialJoinCreation(true, run);
        }, cancellationToken);
    }

    public async Task<BackgroundJobRun?> GetAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await ReadRunAsync(connection, null, id, cancellationToken);
    }

    public async Task<BackgroundJobRun?> GetActiveAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return (await ReadRunsAsync(connection, null,
            $"SELECT TOP (1) {Columns} FROM dbo.BackgroundJobRuns WHERE {ActivePredicate} ORDER BY Id;", [], cancellationToken)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<BackgroundJobRun>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await ReadRunsAsync(connection, null, $"""
            SELECT {Columns} FROM dbo.BackgroundJobRuns WHERE JobType = 'SpatialJoin'
            ORDER BY Id DESC OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY;
            """, [new("@Offset", SqlDbType.BigInt) { Value = ((long)page - 1) * pageSize },
                new("@Take", SqlDbType.Int) { Value = pageSize + 1 }], cancellationToken);
    }

    public async Task<IReadOnlyList<BackgroundJobRun>> GetRecoverableAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await ReadRunsAsync(connection, null, $"""
            SELECT {Columns} FROM dbo.BackgroundJobRuns WHERE {ActivePredicate}
              AND (HangfireJobId IS NOT NULL OR CreatedAt <= DATEADD(second, -60, SYSUTCDATETIME()))
            ORDER BY Id;
            """, [], cancellationToken);
    }

    public async Task<IReadOnlyList<SpatialJoinAssociationDto>> GetAssociationsAsync(SpatialJoinAssociationQueryDto query, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = Command(connection, null, """
            SELECT TOP (@Take) ti.Trail_Id, t.Trail_Name, ti.IndicatorId, i.IndicatorName,
                ti.DistanceMeters, ti.IndicatorWeightSnapshot, ti.EvaluatedAt
            FROM dbo.TrailIndicators ti
            JOIN dbo.Trails t ON t.Trail_Id = ti.Trail_Id
            JOIN dbo.Indicators i ON i.IndicatorId = ti.IndicatorId
            WHERE ti.EvaluatedScore IS NULL
              AND (@TrailId IS NULL OR ti.Trail_Id = @TrailId)
              AND (@IndicatorId IS NULL OR ti.IndicatorId = @IndicatorId)
              AND (ti.Trail_Id > @AfterTrailId OR (ti.Trail_Id = @AfterTrailId AND ti.IndicatorId > @AfterIndicatorId))
            ORDER BY ti.Trail_Id, ti.IndicatorId;
            """, [new("@Take", SqlDbType.Int) { Value = query.PageSize + 1 },
                NullableId("@TrailId", query.TrailId), NullableId("@IndicatorId", query.IndicatorId),
                new("@AfterTrailId", SqlDbType.BigInt) { Value = query.AfterTrailId ?? 0 },
                new("@AfterIndicatorId", SqlDbType.BigInt) { Value = query.AfterIndicatorId ?? 0 }]);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<SpatialJoinAssociationDto>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetInt64(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetDecimal(4), reader.GetDecimal(5), Utc(reader.GetDateTime(6))));
        return result;
    }

    public Task<bool> BindAsync(long id, string jobId, bool start, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        if (jobId.Length > 100) throw new ArgumentOutOfRangeException(nameof(jobId));
        return WithLockAsync(async (connection, transaction) =>
        {
            var run = await ReadRunAsync(connection, transaction, id, cancellationToken);
            if (run is null || SpatialJoinRules.IsTerminal(run.Status) || (run.HangfireJobId is not null && run.HangfireJobId != jobId))
                return false;
            run.HangfireJobId ??= jobId;
            if (start)
            {
                run.Status = "Processing";
                run.StartedAt ??= DateTime.UtcNow;
                run.ErrorMessage = null;
            }
            else if (run.Status == "Pending") run.Status = "Queued";
            await WriteRunAsync(connection, transaction, run, cancellationToken);
            return true;
        }, cancellationToken);
    }

    public Task<bool> SynchronizeAsync(long id, string jobId, CancellationToken cancellationToken) =>
        WithLockAsync(async (connection, transaction) =>
        {
            var run = await ReadRunAsync(connection, transaction, id, cancellationToken);
            if (run is null || SpatialJoinRules.IsTerminal(run.Status) || run.HangfireJobId != jobId) return false;
            if (!SpatialJoinRules.IsValidDistance(run.DistanceMeters)) throw new InvalidOperationException("Invalid persisted spatial distance.");
            // Recheck execution ownership after the short Processing transaction, including on recovery.
            run.Status = "Processing";
            run.StartedAt ??= DateTime.UtcNow;
            await WriteRunAsync(connection, transaction, run, cancellationToken);
            await using var command = Command(connection, transaction, SynchronizationSql,
                [new("@RunId", SqlDbType.BigInt) { Value = id }, JobIdParameter(jobId), DistanceParameter(run.DistanceMeters)]);
            command.CommandTimeout = _options.SyncCommandTimeoutSeconds;
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }, cancellationToken);

    public async Task RecordAttemptErrorAsync(long id, string jobId, string message, CancellationToken cancellationToken) =>
        await WithLockAsync(async (connection, transaction) =>
        {
            var run = await ReadRunAsync(connection, transaction, id, cancellationToken);
            if (run is null || SpatialJoinRules.IsTerminal(run.Status) || run.HangfireJobId != jobId) return false;
            run.ErrorMessage = message;
            await WriteRunAsync(connection, transaction, run, cancellationToken);
            return true;
        }, cancellationToken);

    public async Task ReconcileAsync(long id, string jobId, Func<string?> readCurrentState, CancellationToken cancellationToken) =>
        await WithLockAsync(async (connection, transaction) =>
        {
            var run = await ReadRunAsync(connection, transaction, id, cancellationToken);
            if (run is null || SpatialJoinRules.IsTerminal(run.Status) || run.HangfireJobId != jobId) return false;
            // Read the current public Hangfire state while holding the business lock, never replay an old event.
            var state = readCurrentState();
            var status = SpatialJoinRules.StatusForHangfireState(state);
            if (status is null) return false;
            run.Status = status;
            if (status == "Processing") run.StartedAt ??= DateTime.UtcNow;
            if (status == "Failed")
            {
                run.FinishedAt = DateTime.UtcNow;
                run.ErrorMessage = state switch
                {
                    "Missing" or "Deleted" => "背景工作已不存在，請重新提出同步請求。",
                    "Succeeded" => "背景工作已結束，但同步結果未確認，請重新提出同步請求。",
                    _ => run.ErrorMessage is null || run.ErrorMessage.Contains("背景工作將", StringComparison.Ordinal)
                        ? "同步工作重試後仍失敗，請洽管理員並重新提出請求。" : run.ErrorMessage
                };
            }
            await WriteRunAsync(connection, transaction, run, cancellationToken);
            return true;
        }, cancellationToken);

    private async Task<T> WithLockAsync<T>(Func<SqlConnection, SqlTransaction, Task<T>> action, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await using (var command = Command(connection, transaction, """
            DECLARE @Result int;
            EXEC @Result = sys.sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = @Timeout, @DbPrincipal = 'public';
            SELECT @Result;
            """, [new("@Resource", SqlDbType.NVarChar, 255) { Value = SpatialJoinRules.LockResource },
                new("@Timeout", SqlDbType.Int) { Value = _options.LockTimeoutMilliseconds }]))
        {
            var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (result < 0) throw new SpatialJoinLockException(result);
        }
        var value = await action(connection, transaction);
        await transaction.CommitAsync(cancellationToken);
        return value;
        // Dispose rolls back on any exception, including cancellation or an ambiguous commit.
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Business database connection string is missing."));
        try { await connection.OpenAsync(cancellationToken); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    private async Task<BackgroundJobRun?> ReadRunAsync(SqlConnection connection, SqlTransaction? transaction, long id, CancellationToken cancellationToken) =>
        (await ReadRunsAsync(connection, transaction, $"SELECT {Columns} FROM dbo.BackgroundJobRuns WHERE Id = @Id AND JobType = 'SpatialJoin';",
            [new("@Id", SqlDbType.BigInt) { Value = id }], cancellationToken)).FirstOrDefault();

    private async Task<IReadOnlyList<BackgroundJobRun>> ReadRunsAsync(SqlConnection connection, SqlTransaction? transaction,
        string sql, SqlParameter[] parameters, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<BackgroundJobRun>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new BackgroundJobRun
            {
                Id = reader.GetInt64(0), JobType = reader.GetString(1), Status = reader.GetString(2),
                HangfireJobId = reader.IsDBNull(3) ? null : reader.GetString(3), DistanceMeters = reader.GetDecimal(4),
                CreatedAt = Utc(reader.GetDateTime(5)), StartedAt = reader.IsDBNull(6) ? null : Utc(reader.GetDateTime(6)),
                FinishedAt = reader.IsDBNull(7) ? null : Utc(reader.GetDateTime(7)), ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        return result;
    }

    private async Task WriteRunAsync(SqlConnection connection, SqlTransaction transaction, BackgroundJobRun run, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction, """
            UPDATE dbo.BackgroundJobRuns SET HangfireJobId = @JobId, Status = @Status,
                StartedAt = COALESCE(StartedAt, @StartedAt), FinishedAt = @FinishedAt, ErrorMessage = @ErrorMessage
            WHERE Id = @Id AND JobType = 'SpatialJoin' AND Status NOT IN ('Succeeded', 'Failed')
                AND (HangfireJobId IS NULL OR HangfireJobId = @JobId);
            """, [new("@Id", SqlDbType.BigInt) { Value = run.Id }, JobIdParameter(run.HangfireJobId!),
                new("@Status", SqlDbType.VarChar, 20) { Value = run.Status },
                new("@StartedAt", SqlDbType.DateTime2) { Scale = 0, Value = (object?)run.StartedAt ?? DBNull.Value },
                new("@FinishedAt", SqlDbType.DateTime2) { Scale = 0, Value = (object?)run.FinishedAt ?? DBNull.Value },
                new("@ErrorMessage", SqlDbType.NVarChar, 500) { Value = (object?)run.ErrorMessage ?? DBNull.Value }]);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1) throw new InvalidOperationException("Spatial run state changed.");
    }

    private SqlCommand Command(SqlConnection connection, SqlTransaction? transaction, string sql, SqlParameter[] parameters)
    {
        var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = _options.CommandTimeoutSeconds };
        command.Parameters.AddRange(parameters);
        return command;
    }

    private static SqlParameter DistanceParameter(decimal distance) =>
        new("@DistanceMeters", SqlDbType.Decimal) { Precision = 12, Scale = 2, Value = distance };
    private static SqlParameter JobIdParameter(string id) => new("@JobId", SqlDbType.VarChar, 100) { Value = id };
    private static SqlParameter NullableId(string name, long? value) => new(name, SqlDbType.BigInt) { Value = (object?)value ?? DBNull.Value };
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static string LoadSynchronizationSql()
    {
        using var stream = typeof(SpatialJoinStore).Assembly.GetManifestResourceStream("prjGoHike.Services.SpatialJoins.Synchronize.sql")
            ?? throw new InvalidOperationException("Spatial synchronization SQL resource is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
