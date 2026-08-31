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

            eventReportList = _db.EventReportComplaints.Select(eventReport => new CEventReportWarp
            {
                EventReports = eventReport
            }).Skip((page - 1) * 10).Take(10).ToList();


            viewModel.PageCount = page;
            viewModel.cEventReportsList = eventReportList;
            viewModel.TotalDataCount = totalDataCount;



            return View(viewModel);
        }
    }
}
