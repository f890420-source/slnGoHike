using Microsoft.AspNetCore.Mvc;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    public class EventDataController : Controller
    {
        private GoHikeDataContext _db;

        public EventDataController(GoHikeDataContext db)
        {
            _db = db;
        }
        public IActionResult ManageEventData()
        {
            List<CEventDataWarp> eventList = _db.EventData.Select(eve => new CEventDataWarp
            {
                EventData = eve
            }).ToList();

            return View(eventList);
        }
    }
}
