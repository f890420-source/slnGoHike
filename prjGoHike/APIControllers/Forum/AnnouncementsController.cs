using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.Models.Dtos.Forum;
using prjGoHike.Dtos;

namespace prjGoHike.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementsController : ControllerBase
    {
        private readonly GoHikeDataContext _context;

        public AnnouncementsController(
            GoHikeDataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnouncements()
        {
            var announcements = await _context.Announcements
                .Where(a => a.Status == 1)
                .OrderByDescending(a => a.CreatedDate)
                .Select(a => new AnnouncementDto
                {
                    AnnouncementId = a.AnnouncementId,
                    Title = a.Title,
                    Content = a.Content,
                    CreatedDate = a.CreatedDate,
                    UpdateDate = a.UpdateDate
                })
                .ToListAsync();

            return Ok(announcements);
        }
    }
}
