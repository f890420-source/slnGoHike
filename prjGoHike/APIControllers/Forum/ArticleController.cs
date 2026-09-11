using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Dtos;
using prjGoHike.Models;
using prjGoHike.Models.Dtos;
using prjGoHike.Models.Dtos.Forum;
using prjGoHike.Services.forum;
namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly GoHikeDataContext _context;
        private readonly CloudinaryService _cloudinaryService;

        public ArticlesController(
            GoHikeDataContext context,
            CloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }
        #region 取得所有文章
        // GET: api/Articles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetArticles()
        {
            var articles = await _context.Articles
                .Where(a => a.Status == 1 || a.Status == 3)
                .Select(a => new ArticleDto
                {
                    ArticleId = a.ArticleId,
                    UserId = a.UserId,
                    CategoryId = a.CategoryId,

                    Title = a.Title,
                    Content = a.Content,

                    CreatedDate = a.CreatedDate,
                    UpdateDate = a.UpdateDate,

                    UserNickname = a.User.Nickname,
                    UserAvatarUrl = a.User.AvatarUrl,

                    Status = a.Status,

                    CategoryName = a.Category.CategoryName,

                    ImagePaths = a.ArticleImages
        .OrderBy(ai => ai.SortOrder)
        .Select(ai => ai.ImagePath)
        .ToList(),

                    LikeCount = a.ArticleLikes.Count,

                    CommentCount = _context.Comments
    .Count(c => c.ArticleId == a.ArticleId),

                    FavoriteCount = _context.Favorites
        .Count(f => f.ArticleId == a.ArticleId)
                })
                .ToListAsync();

            return Ok(articles);
        }
        #endregion

        #region 透過ID拿文章
        // GET: api/Articles/2
        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleDto>> GetArticle(int id)
        {
            var article = await _context.Articles
                .Where(a => a.ArticleId == id)
                .Select(a => new ArticleDto
                {
                    ArticleId = a.ArticleId,
                    UserId = a.UserId,
                    CategoryId = a.CategoryId,
                    Title = a.Title,
                    Content = a.Content,
                    CreatedDate = a.CreatedDate,
                    UpdateDate = a.UpdateDate,
                    Status = a.Status,
                    CategoryName = a.Category.CategoryName,

                    ImagePaths = a.ArticleImages
                        .OrderBy(image => image.SortOrder)
                        .Select(image => image.ImagePath)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (article == null)
            {
                return NotFound();
            }

            return Ok(article);
        }
        #endregion

        #region 發文
        // POST: api/Articles
        [HttpPost]
        public async Task<ActionResult<ArticleDto>> CreateArticle(
            [FromForm] CreateArticleDto dto)
        {
            // 1. 建立文章
            var article = new Article
            {
                // TODO: 之後改成從登入會員 Claims 取得
                UserId = 15,

                CategoryId = dto.CategoryId,
                Title = dto.Title,
                Content = dto.Content,
                CreatedDate = DateTime.Now,
                UpdateDate = null,
                Status = 1
            };

            _context.Articles.Add(article);

            // 先存一次，取得 ArticleId
            await _context.SaveChangesAsync();

            // 2. 處理圖片
            if (dto.Images != null && dto.Images.Count > 0)
            {
                int sortOrder = 1;

                foreach (var image in dto.Images)
                {
                    if (image.Length == 0)
                    {
                        continue;
                    }

                    // 3. 上傳圖片到 Cloudinary
                    var imageUrl =
                        await _cloudinaryService.UploadImageAsync(
                            image,
                            "gohike/articles"
                        );

                    // 4. 存 Cloudinary URL 到 ArticleImage
                    var articleImage = new ArticleImage
                    {
                        ArticleId = article.ArticleId,

                        ImagePath = imageUrl,

                        SortOrder = sortOrder,

                        CreatedDate = DateTime.Now
                    };

                    _context.ArticleImages.Add(articleImage);

                    sortOrder++;
                }

                await _context.SaveChangesAsync();
            }

            // 5. 回傳新增完成的文章
            var result = await _context.Articles
                .Where(a => a.ArticleId == article.ArticleId)
                .Select(a => new ArticleDto
                {
                    ArticleId = a.ArticleId,
                    UserId = a.UserId,
                    CategoryId = a.CategoryId,
                    Title = a.Title,
                    Content = a.Content,
                    CreatedDate = a.CreatedDate,
                    UpdateDate = a.UpdateDate,
                    Status = a.Status,
                    CategoryName = a.Category.CategoryName,

                    ImagePaths = a.ArticleImages
                        .OrderBy(ai => ai.SortOrder)
                        .Select(ai => ai.ImagePath)
                        .ToList()
                })
                .FirstAsync();

            return CreatedAtAction(
                nameof(GetArticle),
                new { id = article.ArticleId },
                result
            );
        }
        #endregion

        #region 取得熱門文章
        // GET: api/Articles/hot
        [HttpGet("hot")]
        public async Task<ActionResult<IEnumerable<HotArticleDto>>> GetHotArticles()
        {
            var hotArticles = await _context.Articles

                // 只取得前台可以看到的文章
                .Where(a => a.Status == 1 || a.Status == 3)

                // 計算每篇文章的互動數
                .Select(a => new
                {
                    a.ArticleId,
                    a.Title,
                    a.CreatedDate,

                    LikeCount = _context.ArticleLikes
                        .Count(l => l.ArticleId == a.ArticleId),

                    FavoriteCount = _context.Favorites
                        .Count(f => f.ArticleId == a.ArticleId),

                    CommentCount = _context.Comments
                        .Count(c => c.ArticleId == a.ArticleId)
                })

                // 熱門分數：
                // 留言 × 3 + 收藏 × 2 + 愛心 × 1
                .OrderByDescending(a =>
                    a.CommentCount * 3 +
                    a.FavoriteCount * 2 +
                    a.LikeCount
                )

                // 分數相同時，較新的文章優先
                .ThenByDescending(a => a.CreatedDate)

                // 最多只需要 3 篇
                .Take(3)

                // 轉成熱門文章專用 DTO
                .Select(a => new HotArticleDto
                {
                    ArticleId = a.ArticleId,
                    Title = a.Title,
                    CreatedDate = a.CreatedDate,
                    LikeCount = a.LikeCount,
                    FavoriteCount = a.FavoriteCount,
                    CommentCount = a.CommentCount
                })

                .ToListAsync();

            return Ok(hotArticles);
        }
        #endregion

        #region 取得登入者的發文
        // GET: api/Articles/my
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetMyArticles()
        {
            const long userId = 15;

            var articles = await _context.Articles
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedDate)
                .Select(a => new ArticleDto
                {
                    ArticleId = a.ArticleId,
                    UserId = a.UserId,

                    UserNickname = a.User.Nickname,
                    UserAvatarUrl = a.User.AvatarUrl,

                    CategoryId = a.CategoryId,
                    CategoryName = a.Category.CategoryName,

                    Title = a.Title,
                    Content = a.Content,

                    CreatedDate = a.CreatedDate,
                    UpdateDate = a.UpdateDate,

                    Status = a.Status,

                    ImagePaths = a.ArticleImages
                        .OrderBy(ai => ai.SortOrder)
                        .Select(ai => ai.ImagePath)
                        .ToList(),

                    LikeCount = a.ArticleLikes.Count,

                    FavoriteCount = _context.Favorites.Count(f =>
                        f.ArticleId == a.ArticleId),

                    CommentCount = _context.Comments.Count(c =>
                        c.ArticleId == a.ArticleId)
                })
                .ToListAsync();

            return Ok(articles);
        }
        #endregion

        #region 修改文章
        // PUT: api/Articles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticle(
            int id,
            [FromBody] UpdateArticleDto dto)
        {
            const long userId = 15;

            var article = await _context.Articles
                .FirstOrDefaultAsync(a =>
                    a.ArticleId == id &&
                    a.UserId == userId);

            if (article == null)
            {
                return NotFound();
            }

            article.CategoryId = dto.CategoryId;
            article.Title = dto.Title;
            article.Content = dto.Content;
            article.UpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion
    }
}
