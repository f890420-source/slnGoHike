using Microsoft.AspNetCore.SignalR;
using prjGoHike.Hubs;

namespace prjGoHike.Services;

public sealed class DisasterAlertRealtimeService
{
    private readonly IHubContext<EventHub> _hubContext;
    private readonly ILogger<DisasterAlertRealtimeService> _logger;

    public DisasterAlertRealtimeService(
        IHubContext<EventHub> hubContext,
        ILogger<DisasterAlertRealtimeService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Call after a successful save. Clients receiving AlertsChanged reload
    /// GET /api/disasteralerts; the notification contains only { alertId }.
    /// </summary>
    public async Task PublishChangedAsync(long alertId)
    {
        // A disconnected HTTP caller must not cancel a notification for saved data.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await _hubContext.Clients.All.SendAsync(
                "AlertsChanged", new { alertId }, timeout.Token);
        }
        catch (Exception ex)
        {
            // Persistence already succeeded; a failed notification cannot undo it.
            _logger.LogWarning(ex, "災害警示變更通知發送失敗 (AlertId: {AlertId})", alertId);
        }
    }
}
