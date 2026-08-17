using prjGoHike.Models;

namespace prjGoHike.EventDataViewModel
{
    public class CEventDataVM
    {
        public List<CEventDataWarp> cEventDatasList { get; set; } = new List<CEventDataWarp>();
        public CEventDataWarp cEvent { get; set; } = new CEventDataWarp();
    }
}
