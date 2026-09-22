using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Hubs;
using prjGoHike.Models;
using prjGoHike.Services;
using System.Security.Claims;

namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticleLikesController : ControllerBase
    {
        private readonly GoHikeDataContext _context;
        private readonly NotificationRealtimeService _notificationRealtimeService;

        public ArticleLikesController(
       GoHikeDataContext context,
       NotificationRealtimeService notificationRealtimeService)
        {
            _context = context;
            _notificationRealtimeService = notificationRealtimeService;
        }

        #region 新增文章按讚
        // POST: api/ArticleLikes/{articleId}
        [Authorize]
        [HttpPost("{articleId}")]
        public async Task<IActionResult> LikeArticle(
            int articleId)
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


            // =========================
            // 檢查是否已按讚
            // =========================
            var exists = await _context.ArticleLikes
                .AnyAsync(x =>
                    x.ArticleId == articleId &&
                    x.UserId == userId
                );

            if (exists)
            {
                return BadRequest("你已經按過讚了");
            }


            // =========================
            // 建立按讚
            // =========================
            var like = new ArticleLike
            {
                ArticleId = articleId,
                UserId = userId,
                CreatedDate = DateTime.Now
            };

            _context.ArticleLikes.Add(like);

            await _context.SaveChangesAsync();


            // =========================
            // 建立文章按讚通知
            // Type 3 = 我的文章收到按讚
            // =========================
            var article = await _context.Articles
                .Where(a => a.ArticleId == articleId)
                .Select(a => new
                {
                    a.UserId
                })
                .FirstOrDefaultAsync();


            // 文章存在，而且不是自己按讚自己的文章
            if (article != null &&
                article.UserId != userId)
            {
                // 檢查這個人是否曾經對這篇文章產生過按讚通知
                var notificationExists =
                    await _context.Notifications
                        .AnyAsync(n =>
                            n.UserId == article.UserId &&
                            n.SenderUserId == userId &&
                            n.ArticleId == articleId &&
                            n.Type == 3
                        );


                // 沒有通知過才建立
                if (!notificationExists)
                {
                    var notification = new Notification
                    {
                        // 收到通知的人 = 文章作者
                        UserId = article.UserId,

                        // 發送通知的人 = 按讚者
                        SenderUserId = userId,

                        ArticleId = articleId,
                        CommentId = null,

                        Type = 3,

                        Message = "對你的文章按讚",

                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };

                    _context.Notifications.Add(notification);

                    await _context.SaveChangesAsync();


                    // SignalR 即時推送通知
                    await _notificationRealtimeService
                        .SendNotificationAsync(notification);
                }
            }

            return Ok();
        }
        #endregion

        #region 取消文章按讚
        // DELETE: api/ArticleLikes/{articleId}
        [Authorize]
        [HttpDelete("{articleId}")]
        public async Task<IActionResult> UnlikeArticle(
            int articleId)
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

            var like = await _context.ArticleLikes
                .FirstOrDefaultAsync(x =>
                    x.ArticleId == articleId &&
                    x.UserId == userId
                );

            if (like == null)
            {
                return NotFound("找不到按讚紀錄");
            }

            _context.ArticleLikes.Remove(like);

            await _context.SaveChangesAsync();

            return Ok();
        }
        #endregion

        #region 取得文章按讚狀態
        // GET: api/ArticleLikes/{articleId}
        [Authorize]
        [HttpGet("{articleId}")]
        public async Task<IActionResult> GetLikeStatus(
            int articleId)
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

            var likeCount = await _context.ArticleLikes
                .CountAsync(x =>
                    x.ArticleId == articleId
                );

            var isLiked = await _context.ArticleLikes
                .AnyAsync(x =>
                    x.ArticleId == articleId &&
                    x.UserId == userId
                );

            return Ok(new
            {
                likeCount,
                isLiked
            });
        }
        #endregion
    }
}
