using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Dtos.Forum;
using prjGoHike.Hubs;
using prjGoHike.Models;
using prjGoHike.Services;
using prjGoHike.Services.forum;
using System.Security.Claims;
namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly GoHikeDataContext _context;
        private readonly IHubContext<CommentHub> _hubContext;
        private readonly NotificationRealtimeService _notificationRealtimeService;
        private readonly CloudinaryService _cloudinaryService;
        private readonly SensitiveWordService _sensitiveWordService;
        private readonly CommentValidationService _commentValidationService;
        private readonly GeminiModerationService _geminiModerationService;

        public CommentsController(
            GoHikeDataContext context,
            CloudinaryService cloudinaryService,
            IHubContext<CommentHub> hubContext,
            NotificationRealtimeService notificationRealtimeService,
            SensitiveWordService sensitiveWordService,
            GeminiModerationService geminiModerationService,
            CommentValidationService commentValidationService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _hubContext = hubContext;
            _notificationRealtimeService = notificationRealtimeService;
            _sensitiveWordService = sensitiveWordService;
            _geminiModerationService = geminiModerationService;
            _commentValidationService = commentValidationService;
        }

        #region 取得文章的留言
        // GET: api/Comments/article/2
        [HttpGet("article/{articleId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByArticle(
            int articleId)
        {
            var comments = await _context.Comments
                .Where(c =>
                    c.ArticleId == articleId &&
                    c.Status == 1
                )
                .OrderBy(c => c.CreatedDate)
           .Select(c => new CommentDto
           {
               CommentId = c.CommentId,
               ArticleId = c.ArticleId,
               UserId = c.UserId,

               UserNickname = c.User.Nickname,
               UserAvatarUrl = c.User.AvatarUrl,

               Content = c.Content,
               ParentCommentId = c.ParentCommentId,
               ReplyToUserId = c.ReplyToUserId,

               // 被回覆者的暱稱
               ReplyToUserNickname = c.ReplyToUserId != null
        ? c.ReplyToUser.Nickname
        : null,

               CreatedDate = c.CreatedDate,
               UpdateDate = c.UpdateDate,
               Status = c.Status,

               ImagePaths = _context.CommentImages
    .Where(ci => ci.CommentId == c.CommentId)
    .Select(ci => ci.ImagePath)
    .ToList()
           })
                .ToListAsync();

            return Ok(comments);
        }
        #endregion

        #region 發布留言
        // POST: api/Comments
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CommentDto>> CreateComment(
            [FromForm] CreateCommentDto dto)
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
            // 留言圖片驗證
            // =========================
            var imageValidationError =
                _commentValidationService
                    .ValidateImages(dto.Images);

            if (imageValidationError != null)
            {
                return BadRequest(imageValidationError);
            }

            // =========================
            // 敏感詞檢查
            // =========================
            if (_sensitiveWordService.ContainsSensitiveWord(dto.Content))
            {
                return BadRequest(
                    "留言包含不適當文字"
                );
            }

            //// =========================
            //// Gemini AI 內容審核
            //// =========================
            //try
            //{
            //    var isSafe =
            //        await _geminiModerationService
            //            .IsContentSafeAsync(dto.Content);

            //    if (!isSafe)
            //    {
            //        return BadRequest(
            //            "留言內容可能包含不適當文字"
            //        );
            //    }
            //}
            //catch (Exception ex)
            //{
            //    // Gemini 額度不足、逾時或服務異常時，
            //    // 不影響留言功能，改由本地敏感詞機制把關
            //    Console.WriteLine(
            //        $"Gemini 內容審核失敗：{ex.Message}"
            //    );
            //}

            // =========================
            // 建立留言
            // =========================
            var comment = new Comment
            {
                ArticleId = dto.ArticleId,

                UserId = userId,

                Content = dto.Content,

                ParentCommentId = dto.ParentCommentId,
                ReplyToUserId = dto.ReplyToUserId,

                CreatedDate = DateTime.Now,
                UpdateDate = null,
                Status = 1
            };

            _context.Comments.Add(comment);

            // 先取得 CommentId
            await _context.SaveChangesAsync();

            // =========================
            // 建立文章留言通知
            // Type 1 = 我的文章收到留言
            // =========================
            if (comment.ParentCommentId == null)
            {
                var article = await _context.Articles
                    .Where(a => a.ArticleId == comment.ArticleId)
                    .Select(a => new
                    {
                        a.UserId
                    })
                    .FirstOrDefaultAsync();

                // 文章存在，而且不是自己留言自己的文章
                if (article != null &&
                    article.UserId != comment.UserId)
                {
                    var notification = new Notification
                    {
                        // 收到通知的人 = 文章作者
                        UserId = article.UserId,

                        // 發送通知的人 = 留言者
                        SenderUserId = comment.UserId,

                        ArticleId = comment.ArticleId,
                        CommentId = comment.CommentId,

                        Type = 1,

                        Message = "在你的文章留下了留言",

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

            // =========================
            // 建立留言回覆通知
            // Type 2 = 我的留言收到回覆
            // =========================
            if (comment.ParentCommentId != null &&
                comment.ReplyToUserId != null)
            {
                // 不是自己回覆自己才建立通知
                if (comment.ReplyToUserId != comment.UserId)
                {
                    var notification = new Notification
                    {
                        // 收到通知的人 = 被回覆的會員
                        UserId = comment.ReplyToUserId.Value,

                        // 發送通知的人 = 回覆者
                        SenderUserId = comment.UserId,

                        ArticleId = comment.ArticleId,
                        CommentId = comment.CommentId,

                        Type = 2,

                        Message = "回覆了你的留言",

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

            // =========================
            // 上傳留言圖片
            // =========================
            var imagePaths = new List<string>();

            if (dto.Images != null && dto.Images.Count > 0)
            {
                foreach (var image in dto.Images)
                {
                    if (image.Length == 0)
                    {
                        continue;
                    }

                    // 上傳到 Cloudinary
                    var imageUrl =
                        await _cloudinaryService.UploadImageAsync(
                            image,
                            "gohike/comments"
                        );

                    // Cloudinary URL 存進 CommentImage
                    var commentImage = new CommentImage
                    {
                        CommentId = comment.CommentId,

                        ImagePath = imageUrl,

                        CreatedDate = DateTime.Now
                    };

                    _context.CommentImages.Add(commentImage);

                    // 回傳給 Angular
                    imagePaths.Add(imageUrl);
                }

                await _context.SaveChangesAsync();
            }


            // =========================
            // 取得留言者資料
            // =========================
            var user = await _context.Users
                .Where(u => u.UserId == comment.UserId)
                .Select(u => new
                {
                    u.Nickname,
                    u.AvatarUrl
                })
                .FirstAsync();


            // =========================
            // 取得被回覆者暱稱
            // =========================
            string? replyToUserNickname = null;

            if (comment.ReplyToUserId != null)
            {
                replyToUserNickname = await _context.Users
                    .Where(u => u.UserId == comment.ReplyToUserId)
                    .Select(u => u.Nickname)
                    .FirstOrDefaultAsync();
            }


            // =========================
            // 建立回傳 DTO
            // =========================
            var result = new CommentDto
            {
                CommentId = comment.CommentId,
                ArticleId = comment.ArticleId,
                UserId = comment.UserId,

                UserNickname = user.Nickname,
                UserAvatarUrl = user.AvatarUrl,

                Content = comment.Content,

                ParentCommentId = comment.ParentCommentId,
                ReplyToUserId = comment.ReplyToUserId,
                ReplyToUserNickname = replyToUserNickname,

                CreatedDate = comment.CreatedDate,
                UpdateDate = comment.UpdateDate,
                Status = comment.Status,

                ImagePaths = imagePaths
            };


            // =========================
            // SignalR 即時推播
            // =========================
            await _hubContext.Clients
                .Group($"Article_{result.ArticleId}")
                .SendAsync(
                    "ReceiveComment",
                    result
                );


            return Ok(result);
        }
        #endregion
    }

}
