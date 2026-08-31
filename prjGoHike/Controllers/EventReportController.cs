using Microsoft.AspNetCore.Mvc;
using prjGoHike.EventReportViewModel;
using prjGoHike.Models;
using prjGoHike.MountainViewModel;

namespace prjGoHike.Controllers
{
    public class EventReportController : Controller
    {
        private readonly GoHikeDataContext _db;

        public EventReportController(GoHikeDataContext db)
        {
            _db = db;
        }

        public IActionResult CreatEventReport(int page = 1)
        {
            int totalDataCount = _db.EventReportComplaints.Count();
            List<CEventReportWarp> eventReportList = new List<CEventReportWarp>();
            CEventReportVM viewModel = new CEventReportVM();

            var dbReports = _db.EventReportComplaints
                       .Skip((page - 1) * 10)
                       .Take(10)
                       .ToList();

            
            eventReportList = dbReports.Select(e => new CEventReportWarp
            {
                EventReports = e
            }).ToList();

            viewModel.PageCount = page;
            viewModel.cEventReportsList = eventReportList;
            viewModel.TotalDataCount = totalDataCount;



            return View(viewModel);
        }
        [HttpGet]
        public IActionResult EditEventReport(int? id)
        {
            var cEventReportWarp = _db.EventReportComplaints.FirstOrDefault(e => e.ReportEventId == id);

            if (cEventReportWarp == null)
            {
                return Json(new { success = false, message = "查不到資料" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    reportEventId = cEventReportWarp.ReportEventId,
                    reportTitle = cEventReportWarp.ReportTitle,
                    reportReason = cEventReportWarp.ReportReason,
                    evidenceUrl = cEventReportWarp.EvidenceUrl,
                    reportStatus = cEventReportWarp.ReportStatus,
                    createdAt = cEventReportWarp.CreatedAt
                }
            });
        }

        [HttpPost]
        public IActionResult EditEventReport(CEventReportVM vm)
        {
            var cEventReportWarp = _db.EventReportComplaints
                .FirstOrDefault(e => e.ReportEventId == vm.cEventReportWarp.ReportEventId);

            if (cEventReportWarp == null)
            {
                return Json(new { success = false, message = "資料庫查詢無資料" });
            }

            cEventReportWarp.ReportTitle = vm.cEventReportWarp.ReportTitle;
            cEventReportWarp.ReportReason = vm.cEventReportWarp.ReportReason;
            cEventReportWarp.EvidenceUrl = vm.cEventReportWarp.EvidenceUrl;
            cEventReportWarp.ReportStatus = vm.cEventReportWarp.ReportStatus;
            
            


            _db.SaveChanges();
            return Json(new { success = true, message = "資料修改成功" });
        }

        [HttpPost]
        public IActionResult DeleteEventReport(int? id)
        {
            var report = _db.EventReportComplaints.FirstOrDefault(e => e.ReportEventId == id);
            if (report == null)
            {
                return Json(new { success = false, message = "找不到資料" });
            }

            _db.EventReportComplaints.Remove(report);
            _db.SaveChanges();
            return Json(new { success = true, message = "成功刪除" });
        }
    }
}
