using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Hubs;
using prjGoHike.Models;
using System.Data;

namespace prjGoHike.Services
{
    public class EventStatusUpdater:BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<EventHub> _eventhub;
        private readonly ILogger<EventStatusUpdater> _logger;
        public EventStatusUpdater(IServiceScopeFactory scopeFactory, IHubContext<EventHub> eventhub, ILogger<EventStatusUpdater> logger)
        {
            _scopeFactory = scopeFactory;
            _eventhub = eventhub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    await UpdateStatusAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("資料更新失敗", ex);
                }

                
                await Task.Delay(TimeSpan.FromMinutes(1),stoppingToken);
            }
        }

        private async Task UpdateStatusAsync(CancellationToken token)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            GoHikeDataContext _db = scope.ServiceProvider.GetRequiredService<GoHikeDataContext>();
            DateTime now = DateTime.Now;

            int endedCount = await _db.EventData
     .Where(e => e.EventEndTime < now
              && e.ActivityStatus != Condition_observe_service.Ended)
     .ExecuteUpdateAsync(s => s.SetProperty(e => e.ActivityStatus, Condition_observe_service.Ended), token);

            int startedCount = await _db.EventData
                .Where(e => e.EventStartTime <= now
                         && e.EventEndTime >= now
                         && e.ActivityStatus != Condition_observe_service.Running
                         && e.ActivityStatus != Condition_observe_service.Ended)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.ActivityStatus, Condition_observe_service.Running), token);

            if (endedCount + startedCount > 0)
            {
                await _eventhub.Clients.All.SendAsync("EventDataChanged", 0, 0, token);
            }
        }
    }
}
