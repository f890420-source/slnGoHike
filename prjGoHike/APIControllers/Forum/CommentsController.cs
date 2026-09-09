using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Dtos;
using prjGoHike.Models;
using prjGoHike.Models.Dtos.Forum;
using Microsoft.AspNetCore.SignalR;
using prjGoHike.Hubs;

namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly GoHikeDataContext _context;
        private readonly IHubContext<CommentHub> _hubContext;

        public CommentsController(
            GoHikeDataContext context,
            IHubContext<CommentHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
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


        // POST: api/Comments
        [HttpPost]
        public async Task<ActionResult<CommentDto>> CreateComment(
    [FromForm] CreateCommentDto dto)
        {
            if (dto.Images.Count > 5)
            {
                return BadRequest(
                    "一則留言最多只能上傳 5 張圖片"
                );
            }

            foreach (var image in dto.Images)
            {
                if (image.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(
                        "單張圖片大小不能超過 5 MB"
                    );
                }
            }

            var allowedExtensions = new[]
            {
    ".jpg",
    ".jpeg",
    ".png",
    ".webp"
};

            foreach (var image in dto.Images)
            {
                var extension = Path
                    .GetExtension(image.FileName)
                    .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(
                        "只允許上傳 jpg、jpeg、png、webp 圖片"
                    );
                }
            }

            var allowedContentTypes = new[]
            {
    "image/jpeg",
    "image/png",
    "image/webp"
};

            foreach (var image in dto.Images)
            {
                if (!allowedContentTypes.Contains(image.ContentType))
                {
                    return BadRequest(
                        "上傳檔案格式不正確"
                    );
                }
            }
            var comment = new Comment
            {
                ArticleId = dto.ArticleId,

                // TODO: 之後改成從登入會員 Claims 取得
                UserId = 15,

                Content = dto.Content,

                ParentCommentId = dto.ParentCommentId,
                ReplyToUserId = dto.ReplyToUserId,

                CreatedDate = DateTime.Now,
                UpdateDate = null,
                Status = 1
            };

            // 後面維持原本程式...

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // 儲存留言圖片
            var imagePaths = new List<string>();

            if (dto.Images != null && dto.Images.Count > 0)
            {
                var uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "comments"
                );

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                foreach (var image in dto.Images)
                {
                    if (image.Length == 0)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(image.FileName);

                    var fileName = $"{Guid.NewGuid()}{extension}";

                    var filePath = Path.Combine(
                        uploadFolder,
                        fileName
                    );

                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    var imagePath = $"/uploads/comments/{fileName}";

                    var commentImage = new CommentImage
                    {
                        CommentId = comment.CommentId,
                        ImagePath = imagePath,
                        CreatedDate = DateTime.Now
                    };

                    _context.CommentImages.Add(commentImage);

                    imagePaths.Add(imagePath);
                }

                await _context.SaveChangesAsync();
            }

            // 取得留言者資料
            var user = await _context.Users
                .Where(u => u.UserId == comment.UserId)
                .Select(u => new
                {
                    u.Nickname,
                    u.AvatarUrl
                })
                .FirstAsync();

            string? replyToUserNickname = null;

            if (comment.ReplyToUserId != null)
            {
                replyToUserNickname = await _context.Users
                    .Where(u => u.UserId == comment.ReplyToUserId)
                    .Select(u => u.Nickname)
                    .FirstOrDefaultAsync();
            }

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

            await _hubContext.Clients
                .Group($"Article_{result.ArticleId}")
                .SendAsync(
                    "ReceiveComment",
                    result
                );

            return Ok(result);
        }
    }
    }
