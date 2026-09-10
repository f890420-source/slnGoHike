using Microsoft.AspNetCore.Mvc;
using prjGoHike.Models;
using prjGoHike.Models.Dtos.Forum;
using Microsoft.EntityFrameworkCore;

namespace prjGoHike.Controllers.Api
{
    [Route("api/Reports")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly GoHikeDataContext _context;

        public ReportController(GoHikeDataContext context)
        {
            _context = context;
        }

        // POST: api/Reports
        [HttpPost]
        public async Task<IActionResult> CreateReport(
            [FromBody] CreateReportDto dto)
        {
            //todo 暫時測試，之後改成 Claims
            const long userId = 15;

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return BadRequest("請填寫檢舉原因");
            }

            // 檢查是否已經檢舉過這篇文章
            var alreadyReported = await _context.Reports
                .AnyAsync(r =>
                    r.UserId == userId &&
                    r.ArticleId == dto.ArticleId);

            if (alreadyReported)
            {
                return Conflict(new
                {
                    message = "你已經檢舉過這篇文章"
                });
            }

            var report = new Report
            {
                UserId = userId,
                ArticleId = dto.ArticleId,
                Reason = dto.Reason.Trim(),

                Reply = null,
                AdminId = null,
                ReviewDate = null,

                Status = 0,
                CreatedDate = DateTime.Now
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "檢舉成功",
                reportId = report.ReportId
            });
        }
    }
}
