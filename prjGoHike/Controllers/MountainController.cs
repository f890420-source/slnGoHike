using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using NetTopologySuite.Geometries;
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
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(m => m.Errors).Select(s => s.ErrorMessage);
                return Json(new { success = false, message = string.Join(",", error) });
            }
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
        [HttpGet]
        public IActionResult EditEvent(int id)
        {
            
            var moun = _db.Mountains.FirstOrDefault(m => m.MountainId == id);
            if (moun != null)
            {
                return Json(
                    new
                    {
                        success = true,
                        data = new
                        {
                            mountainId = moun.MountainId,
                            mountainName = moun.MountainName,
                            location = moun.Location,
                            altitude = moun.Altitude,
                            difficultyLevel = moun.DifficultyLevel,
                            mountainsPermitRequired = moun.MountainsPermitRequired,
                            nationalParkPermitRequired = moun.NationalParkPermitRequired
                        }
                    });
            }
            else
            {
                return Json(
                    new { success = false, message = "找不到資料" }
                    );
            }
        }
        
        [HttpPost]
        public IActionResult EditEvent(CMountainVM cMountainVM)
        {
            try
            {
                
                var moun = _db.Mountains.FirstOrDefault(m => m.MountainId == cMountainVM.MountainW.MountainId);
                if (moun != null)
                {
                    moun.MountainName = cMountainVM.MountainW.MountainName;
                    moun.Location = cMountainVM.MountainW.Location;
                    moun.Altitude = cMountainVM.MountainW.Altitude;
                    moun.DifficultyLevel = cMountainVM.MountainW.DifficultyLevel;
                    moun.MountainsPermitRequired = cMountainVM.MountainW.MountainsPermitRequired;
                    moun.NationalParkPermitRequired = cMountainVM.MountainW.NationalParkPermitRequired;

                    _db.SaveChanges();
                    return Json(new { success = true, message = "資料修改成功！" });
                }

                return Json(new { success = false, message = "找不到要修改的資料" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult DeleteEvent(int? id)
        {
            try
            {
                var moun = _db.Mountains.FirstOrDefault(m => m.MountainId == id);
                if (id != null)
                {
                    
                    _db.Remove(moun);
                    _db.SaveChanges();
                }

                else
                {
                    return Json(new { success = false, message = "無資料" });
                }
                if(moun == null)
                {
                    return Json(new { success = false, message = "找不到資料庫資料" });
                }
            }


            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            return Json(new { success = false, message = "資料刪除成功" });
        }
    }   
}
