using Hangfire;
using Microsoft.Extensions.Options;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;

namespace prjGoHike.Services.SpatialJoins;

public interface ISpatialJoinService
{
    Task<(bool Created, SpatialJoinRunDto Run)> CreateAsync(decimal distance, CancellationToken cancellationToken);
    Task<SpatialJoinRunDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<SpatialJoinPageDto<SpatialJoinRunDto>> ListAsync(SpatialJoinQueryDto query, CancellationToken cancellationToken);
    Task<SpatialJoinPageDto<SpatialJoinAssociationDto>> GetAssociationsAsync(SpatialJoinAssociationQueryDto query, CancellationToken cancellationToken);
}

public sealed class SpatialJoinService(ISpatialJoinStore store, IOptions<SpatialJoinOptions> options,
    ILogger<SpatialJoinService> logger, IBackgroundJobClient? jobs = null, JobStorage? storage = null) : ISpatialJoinService
{
    public async Task<(bool Created, SpatialJoinRunDto Run)> CreateAsync(decimal distance, CancellationToken cancellationToken)
    {
        if (!SpatialJoinRules.IsValidDistance(distance)) throw new ArgumentOutOfRangeException(nameof(distance));
        if (!options.Value.Enabled || jobs is null) throw new SpatialJoinUnavailableException();
        SpatialJoinCreation creation;
        try { creation = await store.CreateAsync(distance, cancellationToken); }
        catch (SpatialJoinLockException)
        {
            var active = await store.GetActiveAsync(cancellationToken);
            if (active is null) throw;
            return (false, ToDto(active));
        }
        if (!creation.Created) return (false, ToDto(creation.Run));
        // Once Pending is committed, HTTP cancellation must not erase the request. Recovery also enqueues it.
        var run = await TryEnqueueAsync(creation.Run, CancellationToken.None);
        return (true, ToDto(run));
    }

    public async Task<SpatialJoinRunDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var run = await store.GetAsync(id, cancellationToken);
        return run is null ? null : ToDto(run);
    }

    public async Task<SpatialJoinPageDto<SpatialJoinRunDto>> ListAsync(SpatialJoinQueryDto query, CancellationToken cancellationToken)
    {
        var runs = await store.ListAsync(query.Page, query.PageSize, cancellationToken);
        return new(runs.Take(query.PageSize).Select(ToDto).ToList(), runs.Count > query.PageSize);
    }

    public async Task<SpatialJoinPageDto<SpatialJoinAssociationDto>> GetAssociationsAsync(SpatialJoinAssociationQueryDto query, CancellationToken cancellationToken)
    {
        var items = await store.GetAssociationsAsync(query, cancellationToken);
        return new(items.Take(query.PageSize).ToList(), items.Count > query.PageSize);
    }

    public async Task RecoverAsync(CancellationToken cancellationToken)
    {
        foreach (var run in await store.GetRecoverableAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (run.HangfireJobId is null) await TryEnqueueAsync(run, cancellationToken);
                else await ReconcileRunAsync(run.Id, run.HangfireJobId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "SpatialJoin recovery failed for RunId {RunId}, JobId {JobId}", run.Id, run.HangfireJobId); }
        }
    }

    public Task ReconcileRunAsync(long id, string jobId, CancellationToken cancellationToken)
    {
        if (storage is null) throw new SpatialJoinUnavailableException();
        return store.ReconcileAsync(id, jobId, () =>
        {
            using var connection = storage.GetConnection();
            var state = connection.GetStateData(jobId);
            if (state is not null) return state.Name;
            // Unavailable storage throws. A successful lookup returning no job confirms it is missing.
            return connection.GetJobData(jobId) is null ? "Missing" : null;
        }, cancellationToken);
    }

    private async Task<BackgroundJobRun> TryEnqueueAsync(BackgroundJobRun run, CancellationToken cancellationToken)
    {
        if (jobs is null || !options.Value.Enabled) throw new SpatialJoinUnavailableException();
        try
        {
            var current = await store.GetAsync(run.Id, cancellationToken);
            if (current is null || SpatialJoinRules.IsTerminal(current.Status) || current.HangfireJobId is not null) return current ?? run;
            // PerformContext and CancellationToken are substituted by Hangfire, only RunId is business input.
            var jobId = jobs.Enqueue<SpatialJoinJob>(job => job.ExecuteAsync(run.Id, null!, CancellationToken.None));
            await store.BindAsync(run.Id, jobId, false, cancellationToken);
            return await store.GetAsync(run.Id, cancellationToken) ?? run;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            // Enqueue or ID binding may have committed. Leave recovery/worker ownership checks to settle it.
            logger.LogWarning(ex, "SpatialJoin enqueue/binding needs recovery for RunId {RunId}", run.Id);
            return run;
        }
    }

    private static SpatialJoinRunDto ToDto(BackgroundJobRun run) => new(run.Id, run.Status, run.DistanceMeters,
        run.CreatedAt, run.StartedAt, run.FinishedAt, run.ErrorMessage, $"/api/admin/spatial-joins/{run.Id}");
}
