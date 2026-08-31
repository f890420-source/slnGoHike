using Microsoft.AspNetCore.Mvc;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    public class EventReportController : Controller
    {
        private readonly GoHikeDataContext _db;

        public EventReportController(GoHikeDataContext db)
        {
            _db = db;
        }

        public IActionResult CreatEventReport()
        {




            return View();
        }
    }
}
