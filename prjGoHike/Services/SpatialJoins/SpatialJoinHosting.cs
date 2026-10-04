using System.Threading.Channels;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Options;

namespace prjGoHike.Services.SpatialJoins;

public static class SpatialJoinHosting
{
    public static IServiceCollection AddSpatialJoins(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SpatialJoinOptions>().Bind(configuration.GetSection(SpatialJoinOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => o.CommandTimeoutSeconds * 1000 > o.LockTimeoutMilliseconds,
                "Hangfire:CommandTimeoutSeconds must exceed the application-lock timeout.")
            .ValidateOnStart();
        services.AddScoped<ISpatialJoinStore, SpatialJoinStore>();
        services.AddScoped<SpatialJoinService>();
        services.AddScoped<ISpatialJoinService>(provider => provider.GetRequiredService<SpatialJoinService>());
        services.AddScoped<SpatialJoinJob>();
        services.AddScoped<SpatialJoinRecoveryJob>();

        var settings = configuration.GetSection(SpatialJoinOptions.SectionName).Get<SpatialJoinOptions>() ?? new();
        if (!settings.Enabled) return services;
        var connectionString = configuration.GetConnectionString("Hangfire");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Hangfire is required when Hangfire:Enabled is true.");

        services.AddSingleton<SpatialJoinStateObserver>();
        services.AddHostedService(provider => provider.GetRequiredService<SpatialJoinStateObserver>());
        services.AddHangfire((provider, config) => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseFilter(provider.GetRequiredService<SpatialJoinStateObserver>())
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = settings.PrepareSchemaIfNecessary,
                TryAutoDetectSchemaDependentOptions = false,
                UseRecommendedIsolationLevel = true,
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(5)
            }));
        services.AddHangfireServer(options => options.WorkerCount = 1);
        services.AddHostedService<SpatialJoinRecoveryScheduler>();
        // No Dashboard endpoint: management access is exclusively through authenticated Admin APIs.
        return services;
    }
}

// Notifications only wake reconciliation. They never write business state inside Hangfire's uncommitted transaction.
public sealed class SpatialJoinStateObserver(IServiceScopeFactory scopes, ILogger<SpatialJoinStateObserver> logger)
    : BackgroundService, IApplyStateFilter
{
    private readonly Channel<(long RunId, string JobId)> _changes = Channel.CreateBounded<(long, string)>(
        new BoundedChannelOptions(256) { SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest });

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.BackgroundJob.Job.Type == typeof(SpatialJoinJob)
            && context.BackgroundJob.Job.Args.FirstOrDefault() is long runId)
            _changes.Writer.TryWrite((runId, context.BackgroundJob.Id));
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var change in _changes.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // Give the state transaction a chance to commit; recovery handles an early/lost notification.
                    await Task.Delay(250, stoppingToken);
                    await using var scope = scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<SpatialJoinService>()
                        .ReconcileRunAsync(change.RunId, change.JobId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex) { logger.LogWarning(ex, "SpatialJoin state notification deferred for RunId {RunId}", change.RunId); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}

// Retry registration without making Web startup depend on Hangfire storage being available.
public sealed class SpatialJoinRecoveryScheduler(IRecurringJobManager recurringJobs, ILogger<SpatialJoinRecoveryScheduler> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    recurringJobs.AddOrUpdate<SpatialJoinRecoveryJob>(SpatialJoinRules.RecoveryJobId,
                        job => job.ExecuteAsync(CancellationToken.None), Cron.Minutely(),
                        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
                }
                catch (Exception ex) { logger.LogError(ex, "SpatialJoin recovery registration failed; retrying in one minute"); }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
