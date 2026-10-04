using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;

namespace prjGoHike.Services.SpatialJoins;

// SQL transaction boundary; substituted only in workflow/HTTP tests.
public interface ISpatialJoinStore
{
    Task<SpatialJoinCreation> CreateAsync(decimal distance, CancellationToken cancellationToken);
    Task<BackgroundJobRun?> GetAsync(long id, CancellationToken cancellationToken);
    Task<BackgroundJobRun?> GetActiveAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BackgroundJobRun>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<BackgroundJobRun>> GetRecoverableAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SpatialJoinAssociationDto>> GetAssociationsAsync(SpatialJoinAssociationQueryDto query, CancellationToken cancellationToken);
    Task<bool> BindAsync(long id, string jobId, bool start, CancellationToken cancellationToken);
    Task<bool> SynchronizeAsync(long id, string jobId, CancellationToken cancellationToken);
    Task RecordAttemptErrorAsync(long id, string jobId, string message, CancellationToken cancellationToken);
    Task ReconcileAsync(long id, string jobId, Func<string?> readCurrentState, CancellationToken cancellationToken);
}

public sealed record SpatialJoinCreation(bool Created, BackgroundJobRun Run);
