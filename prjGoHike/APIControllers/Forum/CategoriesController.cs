using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using prjGoHike.Models;
using prjGoHike.Models.Dtos.Forum;

namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly GoHikeDataContext _context;

        public CategoriesController(GoHikeDataContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName
                })
                .ToListAsync();

            return Ok(categories);
        }
    }
}
