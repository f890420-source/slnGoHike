using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.ViewModels;
using System.Collections;

namespace prj.Controllers
{
    //TODO: 表單驗證安全細節
    public class AdminRiskinController : Controller
    {
        private readonly GoHikeDataContext _context;

        public AdminRiskinController(GoHikeDataContext context)
        {
            _context = context;
        }

        // GET: AdminRiskinController
        public async Task<IActionResult> Index()
        {
            var riskIndicator = await _context.RiskIndicators.ToListAsync();
            var rwList = riskIndicator.Select(t => new CRiskIndicatorsWrap(t)).ToList();
            return View(rwList);
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RiskIndicator riskIndicator)
        {
            if (ModelState.IsValid)
            {
                _context.Add(riskIndicator);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var riskindb = await _context.RiskIndicators.FirstOrDefaultAsync(x => x.RiskIndicatorId == id);
            if (riskindb == null)
            {
                return NotFound();
            }
            CRiskIndicatorsWrap rw = new CRiskIndicatorsWrap()
            {
                riskIndicator = riskindb
            };
            return View(rw);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, RiskIndicator riskIndicator)
        {
            if (id != riskIndicator.RiskIndicatorId || !ModelState.IsValid)
            {
                return NotFound();
            }
            try
            {
                _context.Update(riskIndicator);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RiskIndicatorExists(riskIndicator.RiskIndicatorId))
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

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var riskindb = await _context.RiskIndicators.FirstOrDefaultAsync(x => x.RiskIndicatorId == id);
            if (riskindb == null)
            {
                return NotFound();
            }
            CRiskIndicatorsWrap rw = new CRiskIndicatorsWrap()
            {
                riskIndicator = riskindb
            };
            return View(rw);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long? id)
        {
            var riskindb = await _context.RiskIndicators.FirstOrDefaultAsync(x => x.RiskIndicatorId == id);
            if (riskindb != null)
            {
                _context.RiskIndicators.Remove(riskindb);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RiskIndicatorExists(long? riskIndicatorId)
        {
            return _context.RiskIndicators.Any(e => e.RiskIndicatorId == riskIndicatorId);
        }
    }
}
