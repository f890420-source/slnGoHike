using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using prjGoHike.Models;
using prjGoHike.MountainViewModel;

namespace prjGoHike.Controllers
{
    public class MountainController : Controller
    {
        private readonly GoHikeDataContext _db;

        // 透過建構子注入，讓系統自動帶入 appsettings.json 的連線設定
        public MountainController(GoHikeDataContext db)
        {
            _db = db;
        }
 
        public IActionResult CreatMountainData()
        {
            // 從資料庫取出所有山岳資料，並轉換成畫面需要的顯示格式
            List<CMountainWarp> mountainList = _db.Mountains
                .Select(mountain => new CMountainWarp
                {
                    Mountains = mountain
                })
                .ToList();

            // 組成 View 要用的 ViewModel
            CMountainVM viewModel = new CMountainVM
            {
                MountainWrapList = mountainList
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CreateNewEvent(CMountainVM cMountainVM)
        {
            try
            {
                Mountain newMountain = new Mountain
                {
                    MountainName = cMountainVM.MountainW.MountainName,
                    Location = cMountainVM.MountainW.Location,
                    Altitude = cMountainVM.MountainW.Altitude,
                    DifficultyLevel = cMountainVM.MountainW.DifficultyLevel,
                    MountainsPermitRequired = cMountainVM.MountainW.MountainsPermitRequired,
                    NationalParkPermitRequired = cMountainVM.MountainW.NationalParkPermitRequired
                };

                _db.Mountains.Add(newMountain);
                _db.SaveChanges();

                return Json(new { success = true, message = "資料建立成功！" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult EditEvent(CMountainVM cMountainVM)
        {
            GoHikeDataContext db = new GoHikeDataContext();
            var vm = db.Mountains.FirstOrDefault(m => m.MountainId == cMountainVM.MountainW.MountainId);
            vm.MountainName = cMountainVM.MountainW.MountainName;
            vm.Location = cMountainVM.MountainW.Location;
            vm.Altitude = cMountainVM.MountainW.Altitude;
            vm.DifficultyLevel = cMountainVM.MountainW.DifficultyLevel;
            vm.MountainsPermitRequired = cMountainVM.MountainW.MountainsPermitRequired;
            vm.NationalParkPermitRequired = cMountainVM.MountainW.NationalParkPermitRequired;
            db.SaveChanges();
            return Json(new { success = true, message = "修改成功!" });
        }
    }
}
