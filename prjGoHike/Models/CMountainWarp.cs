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
        public string MountainName
        {
            get { return _Mountain.MountainName; }
            set { _Mountain.MountainName = value; }
        }
        public string Location
        {
            get { return _Mountain.Location; }
            set { _Mountain.Location = value; }
        }
        public int Altitude
        {
            get { return _Mountain.Altitude; }
            set { _Mountain.Altitude = value; }
        }
        public int DifficultyLevel
        {
            get { return _Mountain.DifficultyLevel; }
            set { _Mountain.DifficultyLevel = value; }
        }
        public int? MountainsPermitRequired
        {
            get { return _Mountain.MountainsPermitRequired; }
            set { _Mountain.MountainsPermitRequired = value; }
        }
        public int? NationalParkPermitRequired
        {
            get { return _Mountain.NationalParkPermitRequired; }
            set { _Mountain.NationalParkPermitRequired = value; }
        }
        
    }
}
