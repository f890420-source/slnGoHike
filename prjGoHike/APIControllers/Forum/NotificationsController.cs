using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Dtos.Forum;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly GoHikeDataContext _context;

        public NotificationsController(
            GoHikeDataContext context)
        {
            _context = context;
        }
        #region 取得未讀通知
        // GET: api/Notifications/unread
        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>>
            GetUnreadNotifications()
        {
            const long userId = 15;

            var notifications = await _context.Notifications

                // 只取得目前使用者的未讀通知
                .Where(n =>
                    n.UserId == userId &&
                    !n.IsRead)

                // 最新通知排前面
                .OrderByDescending(n => n.CreatedDate)

                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,

                    SenderUserId = n.SenderUserId,

                    SenderNickname =
                        n.SenderUser.Nickname,

                    SenderAvatarUrl =
                        n.SenderUser.AvatarUrl,

                    ArticleId = n.ArticleId,

                    CommentId = n.CommentId,

                    Type = n.Type,

                    Message = n.Message,

                    IsRead = n.IsRead,

                    CreatedDate = n.CreatedDate
                })

                .ToListAsync();

            return Ok(notifications);
        }
        #endregion

        #region 標記通知為已讀
        // PUT: api/Notifications/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            const long userId = 15;

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId == id &&
                    n.UserId == userId);

            if (notification == null)
            {
                return NotFound("找不到通知");
            }

            // 已讀
            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion
    }
}
