using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly GoHikeDataContext _context;

        public FavoritesController(
            GoHikeDataContext context)
        {
            _context = context;
        }

        // POST: api/Favorites/7
        [HttpPost("{articleId}")]
        public async Task<IActionResult> AddFavorite(
            int articleId)
        {
            // TODO: 之後改成登入會員 Claims
            long userId = 15;

            var exists = await _context.Favorites
                .AnyAsync(x =>
                    x.ArticleId == articleId &&
                    x.UserId == userId
                );

            if (exists)
            {
                return BadRequest("你已經收藏過這篇文章");
            }

            var favorite = new Favorite
            {
                ArticleId = articleId,
                UserId = userId,
                CreatedDate = DateTime.Now
            };

            _context.Favorites.Add(favorite);

            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/Favorites/7
        [HttpDelete("{articleId}")]
        public async Task<IActionResult> RemoveFavorite(
            int articleId)
        {
            // TODO: 之後改成登入會員 Claims
            long userId = 15;

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(x =>
                    x.ArticleId == articleId &&
                    x.UserId == userId
                );

            if (favorite == null)
            {
                return NotFound("找不到收藏紀錄");
            }

            _context.Favorites.Remove(favorite);

            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: api/Favorites/7
        [HttpGet("{articleId}")]
        public async Task<IActionResult> GetFavoriteStatus(
            int articleId)
        {
            // TODO: 之後改成登入會員 Claims
            long userId = 15;

            var isFavorited = await _context.Favorites
                .AnyAsync(x =>
                    x.ArticleId == articleId &&
                    x.UserId == userId
                );

            return Ok(new
            {
                isFavorited
            });
        }
    }
}
