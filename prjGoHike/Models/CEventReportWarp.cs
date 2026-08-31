using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
        [DisplayName("使用者ID")]
        public long UserId { get; set; }
        //來自活動主表
        [DisplayName("活動ID")]
        public long EventId { get; set; }
        [DisplayName("回報原因")]
        
        public string ReportReason
        {
            get { return _eventReport.ReportReason; }
            set { _eventReport.ReportReason = value; }
        }
        [DisplayName("檢舉圖片")]
        public string EvidenceUrl
        {
            get { return _eventReport.EvidenceUrl; }
            set { _eventReport.EvidenceUrl = value; }
        }
        [DisplayName("處理狀態")]
        [Required(ErrorMessage ="不能為空")]
        public string ReportStatus
        {
            get { return _eventReport.ReportStatus; }
            set { _eventReport.ReportStatus = value; }
        }
        [DisplayName("案件成立時間")]
        public DateTime CreatedAt
        {
            get { return _eventReport.CreatedAt; }
            set { _eventReport.CreatedAt = value; }
        }
        [DisplayName("檢舉標題")]
        public string ReportTitle
        {
            get { return _eventReport.ReportTitle; }
            set { _eventReport.ReportTitle = value; }
        }
    }
}
