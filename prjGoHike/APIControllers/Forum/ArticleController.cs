using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.Models.Dtos.Forum;

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
                    CategoryName = a.Category.CategoryName
                })
                .FirstOrDefaultAsync();

            if (article == null)
            {
                return NotFound();
            }

            return Ok(article);
        }
    }
}
