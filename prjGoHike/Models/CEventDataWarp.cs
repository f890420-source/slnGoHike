using prjGoHike.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.Models
{
    public class CEventDataWarp
    {
        private EventData _EventData;
        public CEventDataWarp()
        {
            _EventData = new EventData();
        }
        public EventData EventData
        {
            get { return _EventData; }
            set { _EventData = value; }
        }
        [Key]
        public long EventId
        {
            get { return _EventData.EventId; }
            set { _EventData.EventId = value; }
        }

        [DisplayName("山岳編號")]
        public long MountainId
        {
            get { return _EventData.MountainId; }
            set { _EventData.MountainId = value; }
        }

        [DisplayName("活動名稱")]
        public string EventName
        {
            get { return _EventData.EventName; }
            set { _EventData.EventName = value; }
        }

        [DisplayName("最大參與人數")]
        public int MaximumNumber
        {
            get { return _EventData.MaximumNumber; }
            set { _EventData.MaximumNumber = value; }
        }

        [DisplayName("活動狀態")]

        public string ActivityStatus
        {
            get { return _EventData.ActivityStatus; }
            set { _EventData.ActivityStatus = value; }
        }

        [DisplayName("活動圖片")]

        public string ActivityPhoto
        {
            get { return _EventData.ActivityPhoto; }
            set { _EventData.ActivityPhoto = value; }
        }

        [DisplayName("描述")]

        public string Description
        {
            get { return _EventData.Description; }
            set { _EventData.Description = value; }
        }

        [DisplayName("活動創建時間")]
        public DateTime EventDate 
                {
            get { return _EventData.EventDate; }
            set { _EventData.EventDate = value; }
        }

        [DisplayName("是否需要入園/入山證")]
        public bool ReviewRequired 
{
            get { return _EventData.ReviewRequired; }
            set { _EventData.ReviewRequired = value; }
        }


        public string ReviewStatus
        {
            get { return _EventData.ReviewStatus; }
            set { _EventData.ReviewStatus = value; }
        }

        public bool HasActiveReport
        {
            get { return _EventData.HasActiveReport; }
            set { _EventData.HasActiveReport = value; }
        }

        public long LeaderUserId 
        {
            get { return _EventData.LeaderUserId; }
            set { _EventData.LeaderUserId = value; }
        }
    }
}
