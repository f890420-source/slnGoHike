using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using prjGoHike.APIControllers.GoHikeSafe;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Hubs;
using prjGoHike.Models;
using prjGoHike.Services;

static class AlertRealtimeChecks
{
    public static void CheckPublication(RecordingAlertHub hub, int countBefore,
        int savesBefore, long id, Action<bool, string> check)
    {
        check(hub.Messages.Count == countBefore + 1, "Successful alert write publishes exactly once.");
        var message = hub.Messages.Last();
        check(message.Method == "AlertsChanged" && message.Arguments.Length == 1,
            "Broadcast uses AlertsChanged with one argument.");
        var payload = message.Arguments.Single()!.AsObject();
        check(id > 0 && payload.Count == 1 && payload["alertId"]!.GetValue<long>() == id,
            "Notification contains only the correct persisted alertId.");
        check(message.Saves == savesBefore + 1, "Notification occurs after the successful save.");
        check(message.Token.CanBeCanceled && !message.CanceledAtSend,
            "Notification has a live, independently cancellable timeout token.");
    }

    public static async Task RunAsync(TestDataContext context, RecordingAlertHub hub,
        RecordingAlertLogger logger,
        Func<HttpMethod, string, JsonNode?, HttpStatusCode, Task<JsonNode?>> send,
        Action<string?> authenticate, Action<bool, string> check)
    {
        authenticate("Admin");
        const string path = "/api/disasteralerts";
        var payload = JsonNode.Parse("""
            {"alertType":"Rain","alertTitle":"Realtime alert","severityLevel":3,
             "effectiveFrom":"2026-09-01T00:00:00","isActive":true}
            """)!.AsObject();
        var created = (await send(HttpMethod.Post, path, payload, HttpStatusCode.Created))!["data"]!;
        var id = created["alertId"]!.GetValue<long>();
        payload["alertId"] = id;
        payload["isActive"] = false;
        await send(HttpMethod.Put, $"{path}/{id}", payload, HttpStatusCode.OK);
        var active = (await send(HttpMethod.Get, path, null, HttpStatusCode.OK))!["data"]!.AsArray();
        check(active.All(x => x!["alertId"]!.GetValue<long>() != id),
            "Deactivation notifies and the authoritative API excludes the alert.");

        context.FailSave = true;
        try
        {
            await send(HttpMethod.Post, path, payload, HttpStatusCode.InternalServerError);
            await send(HttpMethod.Put, $"{path}/{id}", payload, HttpStatusCode.InternalServerError);
            await send(HttpMethod.Delete, $"{path}/{id}", null, HttpStatusCode.InternalServerError);
        }
        finally { context.FailSave = false; }

        hub.FailSend = true;
        var warningsBefore = logger.Warnings.Count;
        long failureId;
        try
        {
            created = (await send(HttpMethod.Post, path, payload, HttpStatusCode.Created))!["data"]!;
            failureId = created["alertId"]!.GetValue<long>();
            payload["alertId"] = failureId;
            await send(HttpMethod.Put, $"{path}/{failureId}", payload, HttpStatusCode.OK);
            await send(HttpMethod.Delete, $"{path}/{failureId}", null, HttpStatusCode.OK);
        }
        finally { hub.FailSend = false; }
        check(logger.Warnings.Count == warningsBefore + 3
            && logger.Warnings.Skip(warningsBefore).All(x => x.AlertId == failureId
                && x.Exception is InvalidOperationException),
            "Failed API notifications log the alert ID and exception without changing successful responses.");

        await CheckMvcAsync(check);
        await CheckRequestCancellationAsync(check);
        await CheckTimeoutAsync(check);
    }

    private static async Task CheckMvcAsync(Action<bool, string> check)
    {
        await using var context = new TestDataContext();
        var hub = new RecordingAlertHub(() => context.Saves);
        var logger = new RecordingAlertLogger();
        var service = new DisasterAlertRealtimeService(hub, logger);
        var controller = new AdminDisAlertController(context, service);
        var alert = new DisasterAlert
        {
            AlertType = "Rain", AlertTitle = "MVC alert", SeverityLevel = 3,
            EffectiveFrom = new DateTime(2026, 9, 1), IsActive = true
        };

        async Task Successful(Func<Task<IActionResult>> action, long? expectedId = null)
        {
            var count = hub.Messages.Count;
            var saves = context.Saves;
            check(await action() is RedirectToActionResult { ActionName: "Index" },
                "MVC successful writes still redirect to Index.");
            CheckPublication(hub, count, saves, expectedId ?? alert.AlertId, check);
        }

        await Successful(() => controller.Create(alert));
        alert.IsActive = false;
        await Successful(() => controller.Edit(alert.AlertId, alert));

        var before = hub.Messages.Count;
        controller.ModelState.AddModelError("AlertTitle", "Invalid");
        check(await controller.Create(alert) is ViewResult, "Invalid MVC create preserves its response.");
        check(await controller.Edit(alert.AlertId, alert) is NotFoundResult,
            "Invalid MVC edit preserves its response.");
        controller.ModelState.Clear();
        check(await controller.Edit(alert.AlertId + 1, alert) is NotFoundResult,
            "Mismatched MVC edit preserves its response.");
        var missing = new DisasterAlert { AlertId = 99999 };
        check(await controller.Edit(missing.AlertId, missing) is NotFoundResult,
            "Missing MVC edit does not notify.");
        check(await controller.DeleteConfirmed(99999) is RedirectToActionResult,
            "Missing MVC delete preserves its response.");
        check(hub.Messages.Count == before, "Rejected MVC writes and missing deletes do not notify.");

        foreach (var action in new Func<Task<IActionResult>>[]
        {
            () => controller.Create(new DisasterAlert { AlertTitle = "Failed MVC create" }),
            () => controller.Edit(alert.AlertId, alert),
            () => controller.DeleteConfirmed(alert.AlertId)
        })
        {
            context.FailSave = true;
            try
            {
                await action();
                check(false, "MVC save failure must retain its exception behavior.");
            }
            catch (DbUpdateException) { }
            finally { context.FailSave = false; }
            check(hub.Messages.Count == before, "Failed MVC saves do not notify.");
        }

        // The memory test double removes eagerly; restore the alert for the success checks.
        context.DisasterAlerts.Add(alert);
        await Successful(() => controller.DeleteConfirmed(alert.AlertId));
        check(!context.DisasterAlerts.Any(x => x.AlertId == alert.AlertId),
            "Successful MVC delete actually removes the alert.");

        hub.FailSend = true;
        var warningsBefore = logger.Warnings.Count;
        alert = new DisasterAlert { AlertType = "Rain", AlertTitle = "Failed push", SeverityLevel = 3 };
        await Successful(() => controller.Create(alert));
        await Successful(() => controller.Edit(alert.AlertId, alert));
        await Successful(() => controller.DeleteConfirmed(alert.AlertId));
        check(logger.Warnings.Count == warningsBefore + 3
            && logger.Warnings.Skip(warningsBefore).All(x => x.AlertId == alert.AlertId
                && x.Exception is InvalidOperationException),
            "MVC send failures are logged and all three writes still redirect successfully.");
    }

    private static async Task CheckRequestCancellationAsync(Action<bool, string> check)
    {
        await using var context = new TestDataContext();
        using var request = new CancellationTokenSource();
        context.AfterSave = request.Cancel;
        var hub = new RecordingAlertHub(() => context.Saves);
        var service = new DisasterAlertRealtimeService(hub, new RecordingAlertLogger());
        var controller = new DisasterAlertsApiController(context,
            NullLogger<DisasterAlertsApiController>.Instance, service);
        var result = await controller.Create(new DisAlertDto
        {
            AlertType = "Rain", AlertTitle = "Disconnected caller", SeverityLevel = 3,
            EffectiveFrom = new DateTime(2026, 9, 1), IsActive = true
        }, request.Token);
        check(request.IsCancellationRequested && result is ObjectResult { StatusCode: 201 },
            "HTTP request cancellation after saving does not change create success.");
        CheckPublication(hub, 0, 0, context.DisasterAlerts.Single().AlertId, check);
        check(hub.Messages.Single().Token != request.Token,
            "Publish uses its own timeout rather than the cancelled HTTP token.");
    }

    private static async Task CheckTimeoutAsync(Action<bool, string> check)
    {
        var hub = new RecordingAlertHub(() => 0) { WaitForCancellation = true };
        var logger = new RecordingAlertLogger();
        var service = new DisasterAlertRealtimeService(hub, logger);
        var elapsed = Stopwatch.StartNew();
        await service.PublishChangedAsync(123).WaitAsync(TimeSpan.FromSeconds(15));
        check(elapsed.Elapsed >= TimeSpan.FromSeconds(4) && hub.Messages.Single().Token.IsCancellationRequested,
            "A blocked send is cancelled by the independent five-second timeout.");
        check(logger.Warnings.Count == 1 && logger.Warnings[0].AlertId == 123
            && logger.Warnings[0].Exception is OperationCanceledException,
            "A timed-out notification logs its alert ID and exception without throwing.");
    }
}

sealed record AlertMessage(string Method, JsonNode?[] Arguments, int Saves,
    CancellationToken Token, bool CanceledAtSend);

sealed class RecordingAlertHub : IHubContext<EventHub>, IHubClients, IClientProxy
{
    private readonly Func<int> _saves;
    public RecordingAlertHub(Func<int> saves) => _saves = saves;
    public List<AlertMessage> Messages { get; } = [];
    public bool FailSend { get; set; }
    public bool WaitForCancellation { get; set; }
    public IHubClients Clients => this;
    public IGroupManager Groups => throw new NotSupportedException();
    public IClientProxy All => this;

    public async Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Messages.Add(new AlertMessage(method, args.Select(x => JsonSerializer.SerializeToNode(x)).ToArray(),
            _saves(), cancellationToken, cancellationToken.IsCancellationRequested));
        if (FailSend) throw new InvalidOperationException("Simulated SignalR failure");
        if (WaitForCancellation) await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    // Only All is supported: these throw if production stops broadcasting to all clients.
    IClientProxy IHubClients<IClientProxy>.AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Client(string connectionId) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Group(string groupName) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.User(string userId) => throw new NotSupportedException();
    IClientProxy IHubClients<IClientProxy>.Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
}

sealed class RecordingAlertLogger : ILogger<DisasterAlertRealtimeService>
{
    public List<(long AlertId, Exception? Exception)> Warnings { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning)
        {
            var fields = (IEnumerable<KeyValuePair<string, object?>>)state!;
            Warnings.Add(((long)fields.Single(x => x.Key == "AlertId").Value!, exception));
        }
    }
}
