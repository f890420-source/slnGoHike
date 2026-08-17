using Microsoft.AspNetCore.Mvc;
using prjGoHike.EventDataViewModel;
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

            CEventDataVM ce = new CEventDataVM
            {
                cEventDatasList = eventList
            };

            return View(ce);
        }
        public IActionResult EditManageEvent(int? id)
        {
            var cEventDataWarp = _db.EventData.FirstOrDefault(e => e.EventId == id);

            if (cEventDataWarp == null)
            {
                return Json(new { success = false, message = "查不到資料" });
            }
            if (id != null)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        eventId = cEventDataWarp.EventId,
                        mountainId = cEventDataWarp.MountainId,
                        eventName = cEventDataWarp.EventName,
                        maximumNumber = cEventDataWarp.MaximumNumber,
                        activityStatus = cEventDataWarp.ActivityStatus,
                        activityPhoto = cEventDataWarp.ActivityPhoto,
                        description = cEventDataWarp.Description,
                        eventDate = cEventDataWarp.EventDate,
                        reviewRequired = cEventDataWarp.ReviewRequired,
                        reviewStatus = cEventDataWarp.ReviewStatus,
                        hasActiveReport = cEventDataWarp.HasActiveReport,
                        leaderUserId = cEventDataWarp.LeaderUserId
                    }
                });
            }
            else
            {
                return Json(new { success = false, message = "找不到id" });
            }



        }
    }
}
