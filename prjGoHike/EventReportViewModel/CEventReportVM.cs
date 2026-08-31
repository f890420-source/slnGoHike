using prjGoHike.Models;

namespace prjGoHike.EventReportViewModel
{
    public class CEventReportVM
    {
        public CEventReportWarp cEventReportWarp { get; set; } = new CEventReportWarp();
        
        public List<CEventReportWarp> cEventReportsList { get; set; } = new List<CEventReportWarp>();


        public int TotalDataCount { get; set; }

        public int PageCount { get; set; }
    }
}
