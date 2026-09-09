using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.Models.Dtos.Forum;
using prjGoHike.Dtos;
namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly GoHikeDataContext _context;

        public ArticlesController(GoHikeDataContext context)
        {
            _context = context;
        }

        // GET: api/Articles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetArticles()
        {
            var articles = await _context.Articles
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
                    CategoryName = a.Category.CategoryName
                })
                .ToListAsync();

            return Ok(articles);
        }


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
                // 實體資料夾：
                // wwwroot/uploads/articles
                var uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "articles"
                );

                // 資料夾不存在就自動建立
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                int sortOrder = 1;

                foreach (var image in dto.Images)
                {
                    if (image.Length == 0)
                    {
                        continue;
                    }

                    // 取得副檔名，例如 .jpg
                    var extension = Path.GetExtension(image.FileName);

                    // 使用 Guid 避免檔名重複
                    var fileName = $"{Guid.NewGuid()}{extension}";

                    var filePath = Path.Combine(
                        uploadFolder,
                        fileName
                    );

                    // 3. 儲存實體圖片
                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    // 4. 存進 ArticleImage 資料表
                    var articleImage = new ArticleImage
                    {
                        ArticleId = article.ArticleId,

                        ImagePath =
                            $"/uploads/articles/{fileName}",

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
                    CategoryName = a.Category.CategoryName
                })
                .FirstAsync();

            return CreatedAtAction(
                nameof(GetArticle),
                new { id = article.ArticleId },
                result
            );
        }
    }
}
