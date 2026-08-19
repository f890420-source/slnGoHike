using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using NetTopologySuite.Geometries;
using prjGoHike.Models;
using prjGoHike.MountainViewModel;
using System.Text.RegularExpressions;

namespace prjGoHike.Controllers
{
    public class MountainController : Controller
    {
        private readonly GoHikeDataContext _db;

        
        public MountainController(GoHikeDataContext db)
        {
            _db = db;
        }

        public IActionResult CreatMountainData()
        {



            List<CMountainWarp> mountainList = _db.Mountains.Select(mountain => new CMountainWarp
                {
                    Mountains = mountain
                })
                .ToList();

            
            CMountainVM viewModel = new CMountainVM
            {
                MountainWrapList = mountainList
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CreateNewMountain(CMountainVM cMountainVM)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(m => m.Errors).Select(s => s.ErrorMessage);
                return Json(new { success = false, message = string.Join(",", error) });
            }
            try
            {
                Mountain newMountain = new Mountain();
                bool isNameExists = _db.Mountains.Any(m => m.MountainName == cMountainVM.MountainW.MountainName);
                if (isNameExists)
                {
                    return Json(new { success = false, message = "山岳名字不可重複" });
                }
                else
                {
                    newMountain.MountainName = cMountainVM.MountainW.MountainName;
                    newMountain.Location = cMountainVM.MountainW.Location;
                    newMountain.Altitude = cMountainVM.MountainW.Altitude;
                    newMountain.DifficultyLevel = cMountainVM.MountainW.DifficultyLevel;
                    newMountain.MountainsPermitRequired = cMountainVM.MountainW.MountainsPermitRequired;
                    newMountain.NationalParkPermitRequired = cMountainVM.MountainW.NationalParkPermitRequired;
                    _db.Mountains.Add(newMountain);
                    _db.SaveChanges();

                    return Json(new { success = true, message = "資料建立成功！" });
                }            
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult EditMountain(int id)
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
                        //匿名型別
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
        public IActionResult EditMountain(CMountainVM cMountainVM)
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
        public IActionResult DeleteMountain(int? id)
        {
            try
            {
                var moun = _db.Mountains.FirstOrDefault(m => m.MountainId == id);
                if (id != null)
                {
                    
                    _db.Remove(moun);
                    _db.SaveChanges();
                    return Json(new { success = true, message = "資料刪除成功" });
                }
                else if (moun == null)
                {
                    return Json(new { success = false, message = "找不到資料庫資料" });
                }
                else
                {
                    return Json(new { success = false, message = "無資料" });
                }
                
            }


            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            
        }
    }   
}
