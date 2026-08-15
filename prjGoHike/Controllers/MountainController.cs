using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using prjGoHike.Models;
using prjGoHike.MountainViewModel;

namespace prjGoHike.Controllers
{
    public class MountainController : Controller
    {
        public IActionResult CreatMountainData()
        {
            
            CMountainVM mountains = new CMountainVM();
            return View(mountains);
        }
        public IActionResult CreateNewEvent()
        {
            return View();
        }
    }
}
