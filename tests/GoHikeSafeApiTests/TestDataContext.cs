using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using prjGoHike.Models;

// A test double for controller/HTTP behavior. It never opens a database connection.
// SQL Server query translation is checked separately; persistence/FK enforcement
// still needs a disposable SQL Server database for an integration test.
sealed class TestDataContext : GoHikeDataContext
{
    private DbSet<Trail> _trails = new MemorySet<Trail>();
    public bool FailTrailRead { get; set; }
    public override DbSet<Trail> Trails
    {
        get => FailTrailRead ? throw new InvalidOperationException("private database details must not reach clients") : _trails;
        set => _trails = value;
    }
    public override DbSet<TrailSegment> TrailSegments { get; set; } = new MemorySet<TrailSegment>();
    public override DbSet<Indicator> Indicators { get; set; } = new MemorySet<Indicator>();
    public override DbSet<IndicatorSegment> IndicatorSegments { get; set; } = new MemorySet<IndicatorSegment>();
    public override DbSet<DisasterAlert> DisasterAlerts { get; set; } = new MemorySet<DisasterAlert>();
    public override DbSet<AlertSegment> AlertSegments { get; set; } = new MemorySet<AlertSegment>();
    public override DbSet<AlertsTrail> AlertsTrails { get; set; } = new MemorySet<AlertsTrail>();
    public override DbSet<HikeRecordDetail> HikeRecordDetails { get; set; } = new MemorySet<HikeRecordDetail>();
    public override DbSet<TrailFeature> TrailFeatures { get; set; } = new MemorySet<TrailFeature>();
    public override DbSet<TrailIndicator> TrailIndicators { get; set; } = new MemorySet<TrailIndicator>();
    public override DbSet<TrailSubscription> TrailSubscriptions { get; set; } = new MemorySet<TrailSubscription>();
    public override DbSet<TripReport> TripReports { get; set; } = new MemorySet<TripReport>();
    public TestDataContext()
    {
        Trails = new MemorySet<Trail>();
        TrailSegments = new MemorySet<TrailSegment>();
        Indicators = new MemorySet<Indicator>();
        IndicatorSegments = new MemorySet<IndicatorSegment>();
        DisasterAlerts = new MemorySet<DisasterAlert>();
        AlertSegments = new MemorySet<AlertSegment>();
        AlertsTrails = new MemorySet<AlertsTrail>();
        HikeRecordDetails = new MemorySet<HikeRecordDetail>();
        TrailFeatures = new MemorySet<TrailFeature>();
        TrailIndicators = new MemorySet<TrailIndicator>();
        TrailSubscriptions = new MemorySet<TrailSubscription>();
        TripReports = new MemorySet<TripReport>();
    }

    public int Saves { get; private set; }
    public bool FailSave { get; set; }
    private long _nextId = 1;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer("Server=localhost;Database=Unused;Integrated Security=true", x => x.UseNetTopologySuite());

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (FailSave) throw new DbUpdateException("private database details must not reach clients");
        Saves++;
        foreach (var trail in Trails)
        {
            if (trail.TrailId == 0) trail.TrailId = _nextId++;
            foreach (var segment in trail.TrailSegments)
            {
                if (segment.TrailSegmentId == 0) segment.TrailSegmentId = _nextId++;
                segment.TrailId = trail.TrailId;
            }
        }
        foreach (var indicator in Indicators)
        {
            if (indicator.IndicatorId == 0) indicator.IndicatorId = _nextId++;
            foreach (var segment in indicator.IndicatorSegments)
            {
                if (segment.IndicatorSegmentId == 0) segment.IndicatorSegmentId = _nextId++;
                segment.IndicatorId = indicator.IndicatorId;
            }
        }
        foreach (var alert in DisasterAlerts)
        {
            if (alert.AlertId == 0) alert.AlertId = _nextId++;
            foreach (var segment in alert.AlertSegments)
            {
                if (segment.AlertSegmentId == 0) segment.AlertSegmentId = _nextId++;
                segment.AlertId = alert.AlertId;
            }
        }
        foreach (var feature in TrailFeatures)
        {
            if (feature.FeatureId == 0) feature.FeatureId = _nextId++;
            feature.Trail = Trails.FirstOrDefault(x => x.TrailId == feature.TrailId)!;
        }
        return Task.FromResult(1);
    }
}

sealed class MemorySet<T> : DbSet<T>, IQueryable<T>, IAsyncEnumerable<T> where T : class
{
    public override Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType => null!;
    private readonly List<T> _items = [];
    private IQueryable<T> Query => new AsyncQuery<T>(_items);
    Type IQueryable.ElementType => typeof(T);
    Expression IQueryable.Expression => Query.Expression;
    IQueryProvider IQueryable.Provider => Query.Provider;
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new AsyncEnumerator<T>(_items.GetEnumerator(), cancellationToken);
    public override EntityEntry<T> Add(T entity) { _items.Add(entity); return null!; }
    public override EntityEntry<T> Remove(T entity) { _items.Remove(entity); return null!; }
    public override void RemoveRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities.ToList()) _items.Remove(entity);
    }
}

sealed class AsyncQuery<T> : EnumerableQuery<T>, IQueryable<T>, IAsyncEnumerable<T>
{
    public AsyncQuery(IEnumerable<T> values) : base(values) { }
    public AsyncQuery(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new AsyncProvider((IQueryProvider)this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator(), cancellationToken);
}

sealed class AsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) =>
        (IQueryable)Activator.CreateInstance(typeof(AsyncQuery<>).MakeGenericType(expression.Type.GetGenericArguments()[0]), expression)!;
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncQuery<TElement>(expression);
    public object? Execute(Expression expression) => inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var value = typeof(IQueryProvider).GetMethod(nameof(Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(resultType).Invoke(inner, [expression]);
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [value])!;
    }
}

sealed class AsyncEnumerator<T>(IEnumerator<T> inner, CancellationToken cancellationToken) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync()
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(inner.MoveNext());
    }
}
