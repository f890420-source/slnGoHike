using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Hubs;
using prjGoHike.Models;

namespace prjGoHike.Services
{
    public class NotificationRealtimeService
    {
        private readonly GoHikeDataContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationRealtimeService(
            GoHikeDataContext context,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }


        // =========================
        // SignalR 即時推送通知
        // =========================
        public async Task SendNotificationAsync(
            Notification notification)
        {
            // =========================
            // 取得通知發送者資料
            // =========================
            var sender = await _context.Users
                .Where(u =>
                    u.UserId == notification.SenderUserId
                )
                .Select(u => new
                {
                    u.Nickname,
                    u.AvatarUrl
                })
                .FirstOrDefaultAsync();

            if (sender == null)
            {
                return;
            }


            // =========================
            // 推送給通知接收者
            // =========================
            await _hubContext.Clients
                .Group($"User_{notification.UserId}")
                .SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        notificationId =
                            notification.NotificationId,

                        senderUserId =
                            notification.SenderUserId,

                        senderNickname =
                            sender.Nickname,

                        senderAvatarUrl =
                            sender.AvatarUrl,

                        articleId =
                            notification.ArticleId,

                        commentId =
                            notification.CommentId,

                        type =
                            notification.Type,

                        message =
                            notification.Message,

                        isRead =
                            notification.IsRead,

                        createdDate =
                            notification.CreatedDate
                    }
                );
        }
    }
}
