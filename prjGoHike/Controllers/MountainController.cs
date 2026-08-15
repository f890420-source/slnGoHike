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
        [HttpPost]
        public IActionResult CreateNewEvent(CMountainVM cMountainVM)
        {
            GoHikeDataContext db = new GoHikeDataContext();
            db.Mountains.Add(cMountainVM.MountainW.Mountains);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
