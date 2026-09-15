using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Dtos;
using prjGoHike.Dtos.Forum;
using prjGoHike.Models;
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

                    Tags = a.Tags
                       .Select(t => t.TagName)
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
                        .ToList(),

                          // 文章標籤
                    Tags = a.Tags
                .Select(tag => tag.TagName)
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
            Console.WriteLine($"收到的 CategoryId = {dto.CategoryId}");
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

            // 2. 處理文章標籤
            if (dto.Tags != null && dto.Tags.Count > 0)
            {
                foreach (var tagName in dto.Tags)
                {
                    var cleanTagName = tagName.Trim();

                    if (string.IsNullOrWhiteSpace(cleanTagName))
                    {
                        continue;
                    }

                    var tag = await _context.Tags
                        .FirstOrDefaultAsync(t => t.TagName == cleanTagName);

                    if (tag == null)
                    {
                        tag = new Tag
                        {
                            TagName = cleanTagName,
                            CreatedDate = DateTime.Now
                        };

                        _context.Tags.Add(tag);
                    }

                    article.Tags.Add(tag);
                }

                await _context.SaveChangesAsync();
            }

            // 3. 處理圖片
            if (dto.Images != null && dto.Images.Count > 0)
            {
                int sortOrder = 1;

                foreach (var image in dto.Images)
                {
                    if (image.Length == 0)
                    {
                        continue;
                    }

                    // 4. 上傳圖片到 Cloudinary
                    var imageUrl =
                        await _cloudinaryService.UploadImageAsync(
                            image,
                            "gohike/articles"
                        );

                    // 5. 存 Cloudinary URL 到 ArticleImage
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

            // 6. 回傳新增完成的文章
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
    .ToList(),

                    Tags = a.Tags
    .Select(t => t.TagName)
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
            var startDate = DateTime.Now.AddDays(-30);

            var hotArticles = await _context.Articles

                // 只取得前台可以看到的文章
                .Where(a =>
                    (a.Status == 1 || a.Status == 3) &&
                    a.CreatedDate >= startDate
                )

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
            [FromForm] UpdateArticleDto dto)
        {
            const long userId = 15;

            var article = await _context.Articles
                .Include(a => a.ArticleImages)
                .FirstOrDefaultAsync(a =>
                    a.ArticleId == id &&
                    a.UserId == userId);

            if (article == null)
            {
                return NotFound();
            }

            // 更新文章基本資料
            article.CategoryId = dto.CategoryId;
            article.Title = dto.Title;
            article.Content = dto.Content;
            article.UpdateDate = DateTime.Now;

            // 找出被刪除的舊圖片
            var imagesToDelete = article.ArticleImages
                .Where(image =>
                    !dto.KeepImagePaths.Contains(image.ImagePath))
                .ToList();

            // 刪除 Cloudinary 圖片
            foreach (var image in imagesToDelete)
            {
                await _cloudinaryService.DeleteImageAsync(
                    image.ImagePath
                );
            }

            // 刪除資料庫圖片紀錄
            _context.ArticleImages.RemoveRange(
                imagesToDelete
            );

            // 計算下一張圖片排序
            var nextSortOrder =
                article.ArticleImages
                    .Where(image =>
                        !imagesToDelete.Contains(image))
                    .Select(image => image.SortOrder)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

            // 上傳新圖片
            foreach (var imageFile in dto.ImageFiles)
            {
                var imageUrl =
                    await _cloudinaryService.UploadImageAsync(
                        imageFile,
                        "gohike/articles"
                    );

                var articleImage = new ArticleImage
                {
                    ArticleId = article.ArticleId,
                    ImagePath = imageUrl,
                    SortOrder = nextSortOrder,
                    CreatedDate = DateTime.Now
                };

                _context.ArticleImages.Add(
                    articleImage
                );

                nextSortOrder++;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region 刪除文章
        // DELETE: api/Articles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            const long userId = 15;

            var article = await _context.Articles
      .Include(a => a.Tags)
      .FirstOrDefaultAsync(a =>
          a.ArticleId == id &&
          a.UserId == userId);

            if (article == null)
            {
                return NotFound();
            }

            // 1. 找出文章所有留言
            var comments = await _context.Comments
                .Where(c => c.ArticleId == id)
                .ToListAsync();

            var commentIds = comments
                .Select(c => c.CommentId)
                .ToList();
            // 2. 刪除與文章或留言相關的通知
            var notifications = await _context.Notifications
                .Where(n =>
                    n.ArticleId == id ||
                    (n.CommentId.HasValue &&
                     commentIds.Contains(n.CommentId.Value)))
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);

            // 2. 找出留言圖片
            var commentImages = await _context.CommentImages
                .Where(ci => commentIds.Contains(ci.CommentId))
                .ToListAsync();

            // 刪除 Cloudinary 上的留言圖片
            foreach (var image in commentImages)
            {
                await _cloudinaryService.DeleteImageAsync(
                    image.ImagePath
                );
            }

            // 刪除資料庫留言圖片紀錄
            _context.CommentImages.RemoveRange(commentImages);

            // 3. 刪留言
            _context.Comments.RemoveRange(comments);

            // 4. 找出文章圖片
            var articleImages = await _context.ArticleImages
                .Where(ai => ai.ArticleId == id)
                .ToListAsync();

            // 刪除 Cloudinary 上的文章圖片
            foreach (var image in articleImages)
            {
                await _cloudinaryService.DeleteImageAsync(
                    image.ImagePath
                );
            }

            // 刪除資料庫圖片紀錄
            _context.ArticleImages.RemoveRange(articleImages);

            // 5. 刪按讚
            var likes = await _context.ArticleLikes
                .Where(al => al.ArticleId == id)
                .ToListAsync();

            _context.ArticleLikes.RemoveRange(likes);

            // 6. 刪收藏
            var favorites = await _context.Favorites
                .Where(f => f.ArticleId == id)
                .ToListAsync();

            _context.Favorites.RemoveRange(favorites);

            // 7. 刪檢舉
            var reports = await _context.Reports
                .Where(r => r.ArticleId == id)
                .ToListAsync();

            _context.Reports.RemoveRange(reports);

            // 8. 解除文章與標籤的關聯
            article.Tags.Clear();

            // 9. 刪除文章瀏覽紀錄
            var articleViews = await _context.ArticleViews
                .Where(av => av.ArticleId == id)
                .ToListAsync();

            _context.ArticleViews.RemoveRange(articleViews);

            // 10. 最後刪文章
            _context.Articles.Remove(article);

            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion
    }
}
