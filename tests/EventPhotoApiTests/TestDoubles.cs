using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using prjGoHike.Hubs;
using prjGoHike.Models;
using prjGoHike.Services.forum;

sealed class EventPhotoContext : GoHikeDataContext
{
    public override DbSet<EventData> EventData { get; set; } = new MemorySet<EventData>();
    public override DbSet<EventRegistrationAndMemberList> EventRegistrationAndMemberLists { get; set; } = new MemorySet<EventRegistrationAndMemberList>();
    public override DbSet<GroupAttendance> GroupAttendances { get; set; } = new MemorySet<GroupAttendance>();
    public override DbSet<EventLeaderRating> EventLeaderRatings { get; set; } = new MemorySet<EventLeaderRating>();
    public override DbSet<EventReportComplaint> EventReportComplaints { get; set; } = new MemorySet<EventReportComplaint>();
    public override DbSet<SuspensionSchedule> SuspensionSchedules { get; set; } = new MemorySet<SuspensionSchedule>();
    public int Saves { get; private set; }
    public bool FailSave { get; set; }

    public EventPhotoContext() => Reset();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer("Server=localhost;Database=Unused;Integrated Security=true");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (FailSave) throw new DbUpdateException("test save failure");
        Saves++;
        foreach (var activity in EventData)
        {
            if (activity.EventId == 0) activity.EventId = Saves;
            foreach (var registration in activity.EventRegistrationAndMemberLists)
            {
                registration.User = new User { UserId = registration.UserId, Nickname = "測試山友" };
                registration.Event = activity;
            }
        }
        return Task.FromResult(1);
    }

    public void Reset()
    {
        EventData = new MemorySet<EventData>();
        EventRegistrationAndMemberLists = new MemorySet<EventRegistrationAndMemberList>();
        GroupAttendances = new MemorySet<GroupAttendance>();
        EventLeaderRatings = new MemorySet<EventLeaderRating>();
        EventReportComplaints = new MemorySet<EventReportComplaint>();
        SuspensionSchedules = new MemorySet<SuspensionSchedule>();
        FailSave = false;
    }

    public EventData Seed(string? photo)
    {
        var activity = new EventData
        {
            EventId = 100, MountainId = 1, LeaderUserId = 42,
            EventName = "Existing event", ActivityStatus = "招募中", ActivityPhoto = photo,
            EventStartTime = DateTime.Now.AddDays(1), EventEndTime = DateTime.Now.AddDays(2),
            MaximumNumber = 5, Mountain = new Mountain { MountainId = 1, MountainName = "測試山岳" }
        };
        EventData.Add(activity);
        return activity;
    }
}

sealed class RecordingCloudinary(Func<int> saves) : CloudinaryService(new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Cloudinary:CloudName"] = "test-cloud",
        ["Cloudinary:ApiKey"] = "test-key",
        ["Cloudinary:ApiSecret"] = "test-secret"
    }).Build())
{
    public const string PhotoUrl = "https://res.cloudinary.com/test-cloud/image/upload/v123/gohike/events/new.png";
    public string Url { get; set; } = PhotoUrl;
    public bool FailUpload { get; set; }
    public bool FailDelete { get; set; }
    public List<(string Folder, string FileName)> Uploads { get; } = [];
    public List<(string Url, int Saves)> Deletions { get; } = [];

    public override async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        Uploads.Add((folder, file.FileName));
        await using var stream = file.OpenReadStream();
        await stream.CopyToAsync(Stream.Null);
        if (FailUpload) throw new InvalidOperationException("private Cloudinary credentials must not reach clients");
        return Url;
    }

    public override Task DeleteImageAsync(string url)
    {
        Deletions.Add((url, saves()));
        if (FailDelete) throw new InvalidOperationException("test cleanup failure");
        return Task.CompletedTask;
    }

    public void Reset()
    {
        Url = PhotoUrl;
        FailUpload = FailDelete = false;
        Uploads.Clear();
        Deletions.Clear();
    }
}

sealed class RecordingEventLogger : ILogger<EventDataAPIController>
{
    public List<LogLevel> Levels { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Levels.Add(level);
}

sealed class EventPhotoHub : IHubContext<EventHub>
{
    public IHubClients Clients { get; } = new EventPhotoClients();
    public IGroupManager Groups => throw new NotSupportedException();
}

sealed class EventPhotoClients : IHubClients, IClientProxy
{
    public int Sends { get; private set; }
    public IClientProxy All => this;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => this;
    public IClientProxy Client(string connectionId) => this;
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => this;
    public IClientProxy Group(string groupName) => this;
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => this;
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => this;
    public IClientProxy User(string userId) => this;
    public IClientProxy Users(IReadOnlyList<string> userIds) => this;
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Sends++;
        return Task.CompletedTask;
    }
}
