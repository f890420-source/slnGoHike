using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticleLikesController : ControllerBase
    {
        private readonly GoHikeDataContext _context;

        public ArticleLikesController(
            GoHikeDataContext context)
        {
            _context = context;
        }

        // POST: api/ArticleLikes/10
        [HttpPost("{articleId}")]
        public async Task<IActionResult> LikeArticle(
            int articleId)
        {
            // TODO: 之後改成登入會員 Claims
            long userId = 15;

            var exists = await _context.ArticleLikes
                .AnyAsync(x =>
                    x.ArticleId == articleId &&
                    x.UserId == userId
                );

            if (exists)
            {
                return BadRequest("你已經按過讚了");
            }

            var like = new ArticleLike
            {
                ArticleId = articleId,
                UserId = userId,
                CreatedDate = DateTime.Now
            };

            _context.ArticleLikes.Add(like);

            await _context.SaveChangesAsync();

            return Ok();
        }


        // DELETE: api/ArticleLikes/2
        [HttpDelete("{articleId}")]
        public async Task<IActionResult> UnlikeArticle(
            int articleId)
        {
            // TODO: 之後改成登入會員 Claims
            long userId = 15;

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


        // GET: api/ArticleLikes/2
        [HttpGet("{articleId}")]
        public async Task<IActionResult> GetLikeStatus(
            int articleId)
        {
            // TODO: 之後改成登入會員 Claims
            long userId = 15;

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
    }
}
