using System.Reflection;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;
using prjGoHike.Services.SpatialJoins;

// Workflow/HTTP substitutes only. The opt-in SQL checks exercise the real store and geography engine.
sealed class MemoryRunStore : ISpatialJoinStore
{
    public Dictionary<long, BackgroundJobRun> Runs { get; } = [];
    public List<SpatialJoinAssociationDto> Associations { get; } = [];
    public bool ThrowOnAccess { get; set; }
    public bool ThrowOnCreateLock { get; set; }
    public bool ThrowOnBind { get; set; }
    public bool ThrowOnSync { get; set; }
    public int SyncCalls { get; private set; }
    public int CreateCalls { get; private set; }

    public Task<SpatialJoinCreation> CreateAsync(decimal distance, CancellationToken cancellationToken)
    {
        CreateCalls++;
        Guard();
        if (ThrowOnCreateLock) throw new SpatialJoinLockException(-1);
        var active = Runs.Values.FirstOrDefault(r => !SpatialJoinRules.IsTerminal(r.Status));
        if (active is not null) return Task.FromResult(new SpatialJoinCreation(false, Copy(active)));
        var run = new BackgroundJobRun { Id = Runs.Count + 1, JobType = "SpatialJoin", Status = "Pending", DistanceMeters = distance, CreatedAt = DateTime.UtcNow };
        Runs.Add(run.Id, run);
        return Task.FromResult(new SpatialJoinCreation(true, Copy(run)));
    }

    public Task<BackgroundJobRun?> GetAsync(long id, CancellationToken cancellationToken)
    {
        Guard();
        return Task.FromResult(Runs.TryGetValue(id, out var run) ? Copy(run) : null);
    }
    public Task<BackgroundJobRun?> GetActiveAsync(CancellationToken cancellationToken)
    {
        Guard();
        var run = Runs.Values.FirstOrDefault(r => !SpatialJoinRules.IsTerminal(r.Status));
        return Task.FromResult(run is null ? null : Copy(run));
    }
    public Task<IReadOnlyList<BackgroundJobRun>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        Guard();
        return Task.FromResult<IReadOnlyList<BackgroundJobRun>>(Runs.Values.OrderByDescending(r => r.Id).Skip((page - 1) * pageSize).Take(pageSize + 1).Select(Copy).ToList());
    }
    public Task<IReadOnlyList<BackgroundJobRun>> GetRecoverableAsync(CancellationToken cancellationToken)
    {
        Guard();
        return Task.FromResult<IReadOnlyList<BackgroundJobRun>>(Runs.Values.Where(r => !SpatialJoinRules.IsTerminal(r.Status)
            && (r.HangfireJobId is not null || r.CreatedAt <= DateTime.UtcNow.AddSeconds(-60))).Select(Copy).ToList());
    }
    public Task<IReadOnlyList<SpatialJoinAssociationDto>> GetAssociationsAsync(SpatialJoinAssociationQueryDto query, CancellationToken cancellationToken)
    {
        Guard();
        return Task.FromResult<IReadOnlyList<SpatialJoinAssociationDto>>(Associations.Take(query.PageSize + 1).ToList());
    }
    public Task<bool> BindAsync(long id, string jobId, bool start, CancellationToken cancellationToken)
    {
        if (ThrowOnBind) throw new TimeoutException("private binding failure");
        if (!Owns(id, jobId, true)) return Task.FromResult(false);
        var run = Runs[id];
        run.HangfireJobId ??= jobId;
        if (start) { run.Status = "Processing"; run.StartedAt ??= DateTime.UtcNow; run.ErrorMessage = null; }
        else if (run.Status == "Pending") run.Status = "Queued";
        return Task.FromResult(true);
    }
    public Task<bool> SynchronizeAsync(long id, string jobId, CancellationToken cancellationToken)
    {
        if (!Owns(id, jobId)) return Task.FromResult(false);
        SyncCalls++;
        if (ThrowOnSync) throw new TimeoutException("secret connection string and SQL must not reach clients");
        Runs[id].Status = "Succeeded";
        Runs[id].FinishedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }
    public Task RecordAttemptErrorAsync(long id, string jobId, string message, CancellationToken cancellationToken)
    {
        if (Owns(id, jobId)) Runs[id].ErrorMessage = message;
        return Task.CompletedTask;
    }
    public Task ReconcileAsync(long id, string jobId, Func<string?> readCurrentState, CancellationToken cancellationToken)
    {
        if (!Owns(id, jobId)) return Task.CompletedTask;
        var state = readCurrentState();
        var status = SpatialJoinRules.StatusForHangfireState(state);
        if (status is not null) Runs[id].Status = status;
        if (status == "Failed") { Runs[id].FinishedAt = DateTime.UtcNow; Runs[id].ErrorMessage = "受控錯誤摘要"; }
        return Task.CompletedTask;
    }
    private bool Owns(long id, string jobId, bool allowUnbound = false) => Runs.TryGetValue(id, out var run)
        && !SpatialJoinRules.IsTerminal(run.Status) && (run.HangfireJobId == jobId || (allowUnbound && run.HangfireJobId is null));
    private void Guard() { if (ThrowOnAccess) throw new TimeoutException("secret database host/SQL details"); }
    private static BackgroundJobRun Copy(BackgroundJobRun r) => new()
    {
        Id = r.Id, JobType = r.JobType, Status = r.Status, HangfireJobId = r.HangfireJobId, DistanceMeters = r.DistanceMeters,
        CreatedAt = r.CreatedAt, StartedAt = r.StartedAt, FinishedAt = r.FinishedAt, ErrorMessage = r.ErrorMessage
    };
}

sealed class FakeJobClient(FakeJobStorage storage) : IBackgroundJobClient
{
    public bool Unavailable { get; set; }
    public Action<string, Job>? OnCreate { get; set; }
    public List<Job> Created { get; } = [];
    public string Create(Job job, IState state)
    {
        if (Unavailable) throw new TimeoutException("private Hangfire connection details");
        Created.Add(job);
        var id = Created.Count.ToString();
        storage.States[id] = state.Name;
        OnCreate?.Invoke(id, job);
        return id;
    }
    public bool ChangeState(string jobId, IState state, string expectedState)
    {
        storage.States[jobId] = state.Name;
        return true;
    }
}

sealed class FakeJobStorage : JobStorage
{
    public Dictionary<string, string> States { get; } = [];
    public bool Unavailable { get; set; }
    public override IStorageConnection GetConnection()
    {
        if (Unavailable) throw new TimeoutException("private storage failure");
        var proxy = DispatchProxy.Create<IStorageConnection, StateConnectionProxy>();
        ((StateConnectionProxy)(object)proxy).States = States;
        return proxy;
    }
    public override IMonitoringApi GetMonitoringApi() => throw new NotSupportedException();
}

public class StateConnectionProxy : DispatchProxy
{
    public Dictionary<string, string> States { get; set; } = [];
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod!.Name switch
    {
        "GetStateData" => States.TryGetValue((string)args![0]!, out var state) ? new StateData { Name = state, Data = new Dictionary<string, string>() } : null,
        "GetJobData" => null,
        "Dispose" => null,
        _ => throw new NotSupportedException(targetMethod.Name)
    };
}
