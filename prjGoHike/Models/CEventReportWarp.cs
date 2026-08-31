namespace prjGoHike.Models
{
    public class CEventReportWarp
    {

        private EventReportComplaint _eventReport;

        public CEventReportWarp()
        {
            _eventReport = new EventReportComplaint();
        }
        public EventReportComplaint EventReports {
        get { return _eventReport; }
            set { _eventReport = value; }
        }
        //因為是前端完成活動後或是活動期間有檢舉功能 所以應該要設計成傳回
        //活動id
        public long ReportEventId
        {
            get { return _eventReport.ReportEventId; }
            set { _eventReport.ReportEventId = value; }
        }
        //來自user表
        public long UserId { get; set; }
        //來自活動主表
        public long EventId { get; set; }

        public string ReportReason
        {
            get { return _eventReport.ReportReason; }
            set { _eventReport.ReportReason = value; }
        }
        public string EvidenceUrl
        {
            get { return _eventReport.EvidenceUrl; }
            set { _eventReport.EvidenceUrl = value; }
        }
        public string ReportStatus
        {
            get { return _eventReport.ReportStatus; }
            set { _eventReport.ReportStatus = value; }
        }
        public DateTime CreatedAt
        {
            get { return _eventReport.CreatedAt; }
            set { _eventReport.CreatedAt = value; }
        }
        public string ReportTitle
        {
            get { return _eventReport.ReportTitle; }
            set { _eventReport.ReportTitle = value; }
        }
    }
}
