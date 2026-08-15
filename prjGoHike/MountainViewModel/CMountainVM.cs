using prjGoHike.Models;

namespace prjGoHike.MountainViewModel
{
    public class CMountainVM
    {
        public IEnumerable<CMountainWarp> MountainWrapList {  get; set; } = new List<CMountainWarp>();
        public CMountainWarp MountainW {  get; set; } = new CMountainWarp();
    }
}
