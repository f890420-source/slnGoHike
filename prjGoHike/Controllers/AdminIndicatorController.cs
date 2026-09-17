using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.ViewModels;
using System.Collections;

namespace prj.Controllers
{
    //TODO: 表單驗證安全細節
    public class AdminIndicatorController : Controller
    {
        private readonly GoHikeDataContext _context;

        public AdminIndicatorController(GoHikeDataContext context)
        {
            _context = context;
        }

        // GET: AdminIndicatorController
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var indicator = await _context.Indicators.ToListAsync();
            var rwList = indicator.Select(t => new CIndicatorsWrap(t)).ToList();
            return View(rwList);
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(Indicator indicator)
        {
            if (ModelState.IsValid)
            {
                _context.Add(indicator);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var riskindb = await _context.Indicators.FirstOrDefaultAsync(x => x.IndicatorId == id);
            if (riskindb == null)
            {
                return NotFound();
            }
            CIndicatorsWrap rw = new CIndicatorsWrap()
            {
                indicator = riskindb
            };
            return View(rw);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int? id, Indicator indicator)
        {
            if (id != indicator.IndicatorId || !ModelState.IsValid)
            {
                return NotFound();
            }
            try
            {
                _context.Update(indicator);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!IndicatorExists(indicator.IndicatorId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var riskindb = await _context.Indicators.FirstOrDefaultAsync(x => x.IndicatorId == id);
            if (riskindb == null)
            {
                return NotFound();
            }
            CIndicatorsWrap rw = new CIndicatorsWrap()
            {
                indicator = riskindb
            };
            return View(rw);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(long? id)
        {
            var riskindb = await _context.Indicators.FirstOrDefaultAsync(x => x.IndicatorId == id);
            if (riskindb != null)
            {
                _context.Indicators.Remove(riskindb);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IndicatorExists(long? indicatorId)
        {
            return _context.Indicators.Any(e => e.IndicatorId == indicatorId);
        }
    }
}
