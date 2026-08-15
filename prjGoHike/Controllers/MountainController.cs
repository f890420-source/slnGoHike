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
        //    public IActionResult CreatMountainData()
        //    {
        //        using (GoHikeDataContext db = new GoHikeDataContext())
        //        {
        //            // 查詢所有山岳資料並包裝為 CMountainWarp 列表
        //            var list = db.Mountains.ToList().Select(m => new CMountainWarp
        //            {
        //                Mountains = m
        //            }).ToList();

        //            // 建立 ViewModel 並賦值
        //            CMountainVM vm = new CMountainVM
        //            {
        //                MountainWrapList = list
        //            };

        //            return View(vm);
        //        }
        //    }
        //    [HttpPost]
        //    public IActionResult CreateNewEvent(CMountainVM cMountainVM)
        //    {
        //        try
        //        {
        //            GoHikeDataContext db = new GoHikeDataContext();


        //            Mountain newMountain = new Mountain
        //            {
        //                MountainName = cMountainVM.MountainW.MountainName,
        //                Location = cMountainVM.MountainW.Location,
        //                Altitude = cMountainVM.MountainW.Altitude,
        //                DifficultyLevel = cMountainVM.MountainW.DifficultyLevel,
        //                MountainsPermitRequired = cMountainVM.MountainW.MountainsPermitRequired,
        //                NationalParkPermitRequired = cMountainVM.MountainW.NationalParkPermitRequired
        //            };


        //            db.Mountains.Add(newMountain);
        //            db.SaveChanges();


        //            return Json(new { success = true, message = "資料建立成功！" });
        //        }
        //        catch (Exception ex)
        //        {

        //            return Json(new { success = false, message = ex.Message });
        //        }
        //    }
        //}
        public IActionResult CreatMountainData()
        {
            // 查詢所有山岳資料並包裝為 CMountainWarp 列表
            var list = _db.Mountains.ToList().Select(m => new CMountainWarp
            {
                Mountains = m
            }).ToList();

            // 建立 ViewModel 並賦值
            CMountainVM vm = new CMountainVM
            {
                MountainWrapList = list
            };

            return View(vm);
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
    }
}
