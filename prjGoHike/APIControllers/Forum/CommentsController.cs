using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Dtos;
using prjGoHike.Models;
using prjGoHike.Models.Dtos.Forum;

namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly GoHikeDataContext _context;

        public CommentsController(GoHikeDataContext context)
        {
            _context = context;
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
                    Content = c.Content,
                    ParentCommentId = c.ParentCommentId,
                    ReplyToUserId = c.ReplyToUserId,
                    CreatedDate = c.CreatedDate,
                    UpdateDate = c.UpdateDate,
                    Status = c.Status
                })
                .ToListAsync();

            return Ok(comments);
        }

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

            var result = new CommentDto
            {
                CommentId = comment.CommentId,
                ArticleId = comment.ArticleId,
                UserId = comment.UserId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                ReplyToUserId = comment.ReplyToUserId,
                CreatedDate = comment.CreatedDate,
                UpdateDate = comment.UpdateDate,
                Status = comment.Status
            };

            return Ok(result);
        }
    }
}
