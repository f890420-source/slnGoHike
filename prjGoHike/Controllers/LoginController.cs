using Microsoft.AspNetCore.Mvc;

namespace prjGoHike.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Login()
        {

            return View();
        }
    }
}
