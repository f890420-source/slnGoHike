using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Dtos.Forum;
using prjGoHike.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        [Authorize]
        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>>
            GetUnreadNotifications()
        {
            // =========================
            // 取得目前登入會員
            // =========================
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !long.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("無法取得登入會員資料");
            }

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
        [Authorize]
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            // =========================
            // 取得目前登入會員
            // =========================
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !long.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("無法取得登入會員資料");
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId == id &&
                    n.UserId == userId);

            if (notification == null)
            {
                return NotFound("找不到通知");
            }

            // 標記為已讀
            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region 全部通知標記為已讀
        // PUT: api/Notifications/read-all
        [Authorize]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            // =========================
            // 取得目前登入會員
            // =========================
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !long.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("無法取得登入會員資料");
            }

            // 取得目前會員所有未讀通知
            var notifications = await _context.Notifications
                .Where(n =>
                    n.UserId == userId &&
                    !n.IsRead)
                .ToListAsync();

            // 全部標記為已讀
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion
    }
}
