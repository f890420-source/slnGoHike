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
        public IActionResult Index()
        {
            var reports = _context.Reports.ToList();
            return View(reports);
        }
    }
}
