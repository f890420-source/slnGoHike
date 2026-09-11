using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.Models.Dtos.Forum;

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



        #region 新增收藏
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
        #endregion

        #region 取消收藏
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
        #endregion

        #region 取得收藏狀態
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
        #endregion

        #region 取得收藏的文章
        // GET: api/Favorites
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetFavorites()
        {
            const long userId = 15; // TODO: 之後改成從 Claims 取得

            var articles = await _context.Favorites
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedDate)
                .Select(f => new ArticleDto
                {
                    ArticleId = f.Article.ArticleId,
                    UserId = f.Article.UserId,
                    CategoryId = f.Article.CategoryId,

                    Title = f.Article.Title,
                    Content = f.Article.Content,

                    CreatedDate = f.Article.CreatedDate,
                    UpdateDate = f.Article.UpdateDate,

                    Status = f.Article.Status,

                    CategoryName =
                        f.Article.Category.CategoryName,

                    UserNickname =
                        f.Article.User.Nickname,

                    UserAvatarUrl =
                        f.Article.User.AvatarUrl,

                    ImagePaths = f.Article.ArticleImages
                        .OrderBy(ai => ai.SortOrder)
                        .Select(ai => ai.ImagePath)
                        .ToList(),

                    LikeCount =
                        f.Article.ArticleLikes.Count,

                    FavoriteCount =
                        _context.Favorites.Count(x =>
                            x.ArticleId == f.ArticleId),

                    CommentCount =
                        _context.Comments.Count(c =>
                            c.ArticleId == f.ArticleId)
                })
                .ToListAsync();

            return Ok(articles);
        }
        #endregion
    }
}
