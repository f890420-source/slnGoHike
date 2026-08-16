using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.ViewModels;

namespace prj.Controllers
{
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
    }
}
