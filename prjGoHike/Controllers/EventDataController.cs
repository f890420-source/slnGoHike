using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            List<CEventDataWarp> eventList = _db.EventData.Include(eve =>eve.Mountain).Select(eve => new CEventDataWarp
            {

                EventData = eve,
                MountainName = eve.Mountain.MountainName,
                DifficultyLevel = eve.Mountain.DifficultyLevel,
                MountainsPermitRequired = eve.Mountain.MountainsPermitRequired,
                NationalParkPermitRequired = eve.Mountain.NationalParkPermitRequired
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
            var cEventDataWarp = _db.EventData.Include(e =>e.Mountain).FirstOrDefault(e => e.EventId == id);

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
                        mountainName = cEventDataWarp.Mountain.MountainName,
                        difficultyLevel = cEventDataWarp.Mountain.DifficultyLevel,
                        eventName = cEventDataWarp.EventName,
                        maximumNumber = cEventDataWarp.MaximumNumber,
                        eventStartTime = cEventDataWarp.EventStartTime,
                        eventEndTime = cEventDataWarp.EventEndTime,
                        activityStatus = cEventDataWarp.ActivityStatus,
                        activityPhoto = cEventDataWarp.ActivityPhoto,
                        description = cEventDataWarp.Description,
                        mountainsPermitRequired = cEventDataWarp.Mountain.MountainsPermitRequired,
                        nationalParkPermitRequired = cEventDataWarp.Mountain.NationalParkPermitRequired,
                        //eventDate = cEventDataWarp.EventDate,
                        //reviewRequired = cEventDataWarp.ReviewRequired,
                        //reviewStatus = cEventDataWarp.ReviewStatus,
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
                cEventDataWarp.Mountain.MountainName = vm.cEvent.MountainName;
                cEventDataWarp.Mountain.DifficultyLevel = vm.cEvent.DifficultyLevel;
                cEventDataWarp.EventName = vm.cEvent.EventName;
                cEventDataWarp.MaximumNumber = vm.cEvent.MaximumNumber;
                cEventDataWarp.EventStartTime = vm.cEvent.EventStartTime;
                cEventDataWarp.EventEndTime = vm.cEvent.EventEndTime;
                cEventDataWarp.ActivityStatus = vm.cEvent.ActivityStatus;
                cEventDataWarp.ActivityPhoto = vm.cEvent.ActivityPhoto;
                cEventDataWarp.Description = vm.cEvent.Description;
                cEventDataWarp.Mountain.MountainsPermitRequired = vm.cEvent.MountainsPermitRequired;
                cEventDataWarp.Mountain.NationalParkPermitRequired = vm.cEvent.NationalParkPermitRequired;
                //cEventDataWarp.EventDate = vm.cEvent.EventDate;
                //cEventDataWarp.ReviewRequired = vm.cEvent.ReviewRequired;
                //cEventDataWarp.ReviewStatus = vm.cEvent.ReviewStatus;
                cEventDataWarp.HasActiveReport = vm.cEvent.HasActiveReport;
                cEventDataWarp.LeaderUserId = vm.cEvent.LeaderUserId;
                _db.SaveChanges();
                return Json(new { success = true, message = "資料修改成功" });
            }
        }
        [HttpPost]
        public IActionResult DeleteEvent(int? id)
        {
            var vm = _db.EventData.FirstOrDefault(e => e.EventId == id);
            if (vm != null)
            {
                
                _db.EventData.Remove(vm);
                _db.SaveChanges();
                return Json(new { success = true, message = "成功刪除" });
            }
            else
            {
                return Json(new { success = false, message = "找不到資料" });
            }

        }
    }
}
