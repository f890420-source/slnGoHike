using Microsoft.AspNetCore.Mvc;

namespace prjGoHike.Controllers
{
    public class MemberAdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
