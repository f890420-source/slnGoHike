using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.Models
{
    public class CMountainWarp
    {
        private Mountain _Mountain;
        public CMountainWarp()
        {
            _Mountain = new Mountain();
        }
        public Mountain Mountains
        {
            get { return _Mountain; }
            set { _Mountain = value; }
        }
        [Key]
        public long MountainId
        {
            get { return _Mountain.MountainId; }
            set { _Mountain.MountainId = value; }
        }
        [DisplayName("山岳名稱")]
        [Required(ErrorMessage = "請輸入山岳名稱")]
        public string MountainName
        {
            get { return _Mountain.MountainName; }
            set { _Mountain.MountainName = value; }
        }
        [DisplayName("地點")]
        [Required(ErrorMessage = "請輸入地點")]
        public string Location
        {
            get { return _Mountain.Location; }
            set { _Mountain.Location = value; }
        }
        [DisplayName("高度")]
        [Required(ErrorMessage = "請輸入高度")]
        public int Altitude
        {
            get { return _Mountain.Altitude; }
            set { _Mountain.Altitude = value; }
        }
        [DisplayName("山岳難度")]
        [Required(ErrorMessage = "請輸入難度")]
        public int DifficultyLevel
        {
            get { return _Mountain.DifficultyLevel; }
            set { _Mountain.DifficultyLevel = value; }
        }
        [DisplayName("入山證")]
        [Required(ErrorMessage = "請輸入是否需要入山證(請填入1或0)")]
        public int? MountainsPermitRequired
        {
            get { return _Mountain.MountainsPermitRequired; }
            set { _Mountain.MountainsPermitRequired = value; }
        }
        [DisplayName("入園證")]
        [Required(ErrorMessage = "請輸入是否需要入園證(請填入1或0)")]
        public int? NationalParkPermitRequired
        {
            get { return _Mountain.NationalParkPermitRequired; }
            set { _Mountain.NationalParkPermitRequired = value; }
        }
        
    }
}
