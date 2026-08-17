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
        [HttpGet]
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
        [HttpPost]
        public IActionResult EditManageEvent(CEventDataVM vm)
        {
            var cEventDataWarp = _db.EventData.FirstOrDefault(e => e.EventId == vm.cEvent.EventId);
            if (cEventDataWarp == null)
            {
                return Json(new { success = false, message = "資料庫查詢無資料" });
            }
            else
            {
                cEventDataWarp.EventId = vm.cEvent.EventId;
                cEventDataWarp.MountainId = vm.cEvent.MountainId;
                cEventDataWarp.EventName = vm.cEvent.EventName;
                cEventDataWarp.MaximumNumber = vm.cEvent.MaximumNumber;
                cEventDataWarp.ActivityStatus = vm.cEvent.ActivityStatus;
                cEventDataWarp.ActivityPhoto = vm.cEvent.ActivityPhoto;
                cEventDataWarp.Description = vm.cEvent.Description;
                cEventDataWarp.EventDate = vm.cEvent.EventDate;
                cEventDataWarp.ReviewRequired = vm.cEvent.ReviewRequired;
                cEventDataWarp.ReviewStatus = vm.cEvent.ReviewStatus;
                cEventDataWarp.HasActiveReport = vm.cEvent.HasActiveReport;
                cEventDataWarp.LeaderUserId = vm.cEvent.LeaderUserId;
                _db.SaveChanges();
                return Json(new { success = true, message = "資料修改成功" });
            }
        }
    }
}
