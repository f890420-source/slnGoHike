using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    public class ReportController : Controller
    {
        private readonly GoHikeDataContext _context;

        public ReportController(GoHikeDataContext context)
        {
            _context = context;
        }
        public IActionResult Index(int? status, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Reports
                .Include(r => r.Article)
                .AsQueryable();

            // 狀態篩選
            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            // 總資料數
            int totalCount = query.Count();

            // 總頁數
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // 防止頁數超出範圍
            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // 分頁
            var reports = query
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Status = status;

            return View(reports);
        }

        public IActionResult Details(int id)
        {
            var report = _context.Reports
                .Include(r => r.Article)
                .Include(r => r.User)
                .Include(r => r.Admin)
                .FirstOrDefault(r => r.ReportId == id);

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }
        [HttpPost]
        public IActionResult ProcessReport(int ReportId, byte Status, string? Reply)
        {
            var report = _context.Reports
                .FirstOrDefault(r => r.ReportId == ReportId);

            if (report == null)
            {
                return NotFound();
            }

            report.Status = Status;
            report.Reply = Reply;
            report.ReviewDate = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var report = _context.Reports
                .FirstOrDefault(r => r.ReportId == id);

            if (report == null)
            {
                return NotFound();
            }

            _context.Reports.Remove(report);

            int result = _context.SaveChanges();

            if (result == 0)
            {
                return BadRequest("刪除失敗");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
