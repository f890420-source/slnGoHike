using Microsoft.AspNetCore.Mvc;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    public class MountainController : Controller
    {
        public IActionResult Home()
        {
            List<Mountain> mountain = new List<Mountain>();
            return View(mountain);
        }
    }
}
