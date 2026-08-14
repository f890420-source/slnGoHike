using Microsoft.AspNetCore.Mvc;
using prjGoHike.Models;

namespace prjGoHike.Controllers
{
    public class MountainController : Controller
    {
        public IActionResult Home()
        {
            
            return View();
        }
    }
}
