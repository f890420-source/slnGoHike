using Microsoft.AspNetCore.Mvc;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    public class MountainController : Controller
    {
        public IActionResult CreatMountainData()
        {
            List<CMountainWarp> mountains = new List<CMountainWarp>();
            return View(mountains);
        }
    }
}
