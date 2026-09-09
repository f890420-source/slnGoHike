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
               Status = c.Status
           })
                .ToListAsync();

            return Ok(comments);
        }


        // POST: api/Comments
        [HttpPost]
        public async Task<ActionResult<CommentDto>> CreateComment(
            CreateCommentDto dto)
        {
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

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

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

                // 加這行
                ReplyToUserNickname = replyToUserNickname,

                CreatedDate = comment.CreatedDate,
                UpdateDate = comment.UpdateDate,
                Status = comment.Status
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
