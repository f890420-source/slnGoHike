using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    public class AnnouncementController : Controller
    {
        private readonly GoHikeDataContext _context;

        public AnnouncementController(GoHikeDataContext context)
        {
            _context = context;
        }

        // 公告列表
        public async Task<IActionResult> Index(int? status, int page = 1)
        {
            int pageSize = 15;

            if (page < 1)
            {
                page = 1;
            }

            var query = _context.Announcements
                .AsQueryable();

            // 狀態篩選
            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            // 排序
            query = query.OrderByDescending(a => a.CreatedDate);

            // 總筆數
            int totalCount = await query.CountAsync();

            // 總頁數
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / pageSize
            );

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // 取得目前頁面的資料
            var announcements = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Status = status;

            return View(announcements);
        }

        // 新增公告頁面
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 新增公告
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                announcement.CreatedDate = DateTime.Now;

                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(announcement);
        }
        // GET: Announcement/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements.FindAsync(id);

            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }


        // POST: Announcement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Announcement announcement)
        {
            if (id != announcement.AnnouncementId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAnnouncement = await _context.Announcements
                        .FirstOrDefaultAsync(a => a.AnnouncementId == id);

                    if (existingAnnouncement == null)
                    {
                        return NotFound();
                    }

                    // 更新標題
                    existingAnnouncement.Title = announcement.Title;

                    // 更新內容
                    existingAnnouncement.Content = announcement.Content;

                    // 更新狀態
                    existingAnnouncement.Status = announcement.Status;

                    // 更新時間
                    existingAnnouncement.UpdateDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Announcements.Any(e => e.AnnouncementId == id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(announcement);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.AnnouncementId == id);

            if (announcement == null)
            {
                return NotFound();
            }

            _context.Announcements.Remove(announcement);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}