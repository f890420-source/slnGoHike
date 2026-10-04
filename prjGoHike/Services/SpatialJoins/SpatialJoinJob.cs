using Hangfire;
using Hangfire.Server;
using Microsoft.Data.SqlClient;

namespace prjGoHike.Services.SpatialJoins;

public sealed class SpatialJoinJob(ISpatialJoinStore store, ILogger<SpatialJoinJob> logger)
{
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ExecuteAsync(long runId, PerformContext executionContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        await ExecuteRunAsync(runId, executionContext.BackgroundJob.Id, cancellationToken);
    }

    // Explicit execution context for focused SQL tests; this is not an HTTP endpoint.
    public async Task ExecuteRunAsync(long runId, string jobId, CancellationToken cancellationToken)
    {
        try
        {
            if (!await store.BindAsync(runId, jobId, true, cancellationToken)) return;
            await store.SynchronizeAsync(runId, jobId, cancellationToken);
            logger.LogInformation("SpatialJoin attempt completed for RunId {RunId}, JobId {JobId}", runId, jobId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "SpatialJoin attempt failed for RunId {RunId}, JobId {JobId}", runId, jobId);
            try { await store.RecordAttemptErrorAsync(runId, jobId, ErrorSummary(ex), CancellationToken.None); }
            catch (Exception statusError) { logger.LogError(statusError, "Could not record attempt error for RunId {RunId}", runId); }
            // Hangfire elects Scheduled/Failed after this throw; do not turn an attempt into a final Failed here.
            throw;
        }
    }

    private static string ErrorSummary(Exception exception) => exception switch
    {
        SqlException { Number: 51001 } => "已發布步道或啟用指標缺少空間 Segment，請補齊資料後重新同步。",
        SqlException { Number: 51002 } => "來源圖形的 SRID、有效性、空值或幾何型別不符合要求，請修正後重新同步。",
        SqlException { Number: 51003 } => "來源空間距離無法計算，請檢查來源資料。",
        SpatialJoinLockException => "同步暫時無法取得交易鎖，背景工作將依設定重試。",
        _ => "同步暫時失敗，背景工作將依設定重試；若持續失敗請洽管理員。"
    };
}

public sealed class SpatialJoinRecoveryJob(SpatialJoinService service)
{
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteAsync(CancellationToken cancellationToken) => service.RecoverAsync(cancellationToken);
}
