using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.ViewModels_user.Member;
using System.Security.Claims;
namespace prjGoHike.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
   
        private readonly GoHikeDataContext _context;
        private readonly ILogger<MemberController> _logger;
        //內建的日誌介面
        public MemberController(GoHikeDataContext context, ILogger<MemberController> logger)
        {
            _context = context;
            _logger = logger;
        }
        /// <summary>
        /// 取得目前登入的使用者 ID
        /// </summary>
        private long GetUserId()
        {
            var UserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //(ClaimTypes.NameIdentifier).NET預設登入後，會自動將使用者的 ID 存入 ClaimTypes.NameIdentifier 中
            return long.Parse(UserIdClaim ?? "0");// 如果找不到使用者 ID，回傳 0
        }
        /// <summary>
        /// 個人資料頁
        /// </summary>
        [HttpGet]
        // async,task,await 非同步程式設計
        public async Task<IActionResult> Profile()
        {
            try
            {
                long userId = GetUserId();
                if (userId == 0)
                    return Unauthorized("請先登入");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                    return NotFound("使用者不存在");

                var viewModel = new MemberProfileViewModel
                {
                    UserId = user.UserId,
                    Nickname = user.Nickname,
                    Email = user.Email,
                    Bio = user.Bio,
                    AvatarUrl = user.AvatarUrl,
                    AvatarBlurState = user.AvatarBlurState,
                    RegionPreference = user.RegionPreference,
                    DifficultyPreference = user.DifficultyPreference,
                    CreatedAt = user.CreatedAt,
                    LastActiveAt = user.LastActiveAt,
                    AccountStatus = user.AccountStatus
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "個人資料頁出錯");
                return StatusCode(500, "系統錯誤");
            }
        }
        /// <summary>
        /// 編輯個人資料
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Profile(MemberProfileViewModel model)
        {
            try
            {
                long userid = GetUserId();
                if (userid == null)
                    return Unauthorized("請先登入");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userid);
                if (user == null)
                    return NotFound("使用者不存在");
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    // 1. 設定圖片儲存路徑 (wwwroot/uploads/avatars)
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // 2. 產生獨一無二的檔名（避免不同人上傳同檔名被覆蓋）
                    string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.AvatarFile.FileName)}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // 3. 將檔案寫入伺服器硬碟
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(fileStream);
                    }

                    // 4. 把產生好的新網址寫入 user.AvatarUrl
                    user.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
                }
                // 只允許修改特定欄位
                user.Nickname = model.Nickname;
                user.Bio = model.Bio ?? "";
                user.RegionPreference = model.RegionPreference ?? "";
                user.DifficultyPreference = model.DifficultyPreference ?? "";
                user.AvatarUrl = model.AvatarUrl ?? "";

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "個人資料已更新";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "編輯個人資料出錯");
                TempData["ErrorMessage"] = "更新失敗,請稍後重試";
                return RedirectToAction(nameof(Profile));
            }
        }

        /// <summary>
        /// 等級進度頁
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Progress()
        {
            try
            {
                long userId = GetUserId();
                if (userId == 0)
                    return Unauthorized("請先登入");

                var user = await _context.Users
                    .Include(u => u.CurrentLevel)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return NotFound("使用者不存在");

                var viewModel = new MemberLevelViewModel
                {
                    Nickname = user.Nickname,
                    LevelName = user.CurrentLevel?.LevelName ?? "未設定",
                    CurrentLevelId = user.CurrentLevelId,
                    TotalXp = user.TotalXp,
                    MinXp = user.CurrentLevel?.MinXp ?? 0,
                    MaxXp = user.CurrentLevel?.MaxXp ?? 1000
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "等級進度頁出錯");
                return StatusCode(500, "系統錯誤");
            }
        }
        /// <summary>
        /// 360度總覽頁(整合所有資訊)
        /// </summary>
        [HttpGet]
        [Route("Member/Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                long userId = GetUserId();
                if (userId == 0)
                    return Unauthorized("請先登入");

                var user = await _context.Users
                    .Include(u => u.CurrentLevel)
                    .Include(u => u.UserAchievements)
                        .ThenInclude(ua => ua.Achievement)
                    .Include(u => u.UserSkillTags)
                        .ThenInclude(ust => ust.SkillTag)
                    .Include(u => u.HikeRecords)
                    .Include(u => u.SuspensionSchedules)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return NotFound("使用者不存在");

                // 取得所有成就用來計算解鎖率
                var totalAchievements = await _context.Achievements.CountAsync();

                var viewModel = new MemberDashboardViewModel
                {
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    AccountStatus = user.AccountStatus,

                    // 等級資訊
                    LevelName = user.CurrentLevel?.LevelName ?? "未設定",
                    TotalXp = user.TotalXp,
                    ProgressPercentage = user.CurrentLevel != null
    ? (decimal)(user.TotalXp - user.CurrentLevel.MinXp) / (user.CurrentLevel.MaxXp - user.CurrentLevel.MinXp) * 100
    : 0,


                    // 成就資訊
                    UnlockedAchievements = user.UserAchievements
                        .Where(ua => ua.Achievement != null)
                        .Select(ua => new AchievementDto
                        {
                            AchievementId = ua.AchievementId,
                            Name = ua.Achievement.Name,
                            Description = ua.Achievement.Description,
                            Rarity = ua.Achievement.Rarity,
                            UnlockedAt = ua.UnlockedAt
                        })
                        .ToList(),
                    TotalAchievementCount = totalAchievements,

                    // 標籤資訊
                    SkillTags = user.UserSkillTags
                        .Where(ust => ust.SkillTag != null)
                        .Select(ust => new SkillTagDto
                        {
                            TagId = ust.TagId,
                            TagName = ust.SkillTag.TagName,
                            Category = ust.SkillTag.Category,
                            Source = ust.Source
                        })
                        .ToList(),

                    // 爬山紀錄資訊
                    HikeRecords = user.HikeRecords
                        .Select(hr => new HikeRecordDto
                        {
                            RecordId = hr.RecordId,
                            MountainId = hr.MountainId,
                            MountainName = "待補(另一組模組)", // 因為 Mountains 不在 User Navigation 裡
                            HikeDate = hr.HikeDate,
                            CompanionCount = hr.CompanionCount,
                            Verified = hr.Verified
                        })
                        .OrderByDescending(h => h.HikeDate)
                        .ToList(),
                    TotalHikeCount = user.HikeRecords.Count,

                    //停權紀錄
                    SuspensionHistory = user.SuspensionSchedules
                        .Select(ss => new SuspensionDto
                        {
                            SuspensionId = ss.BanId,
                            Reason = ss.Reason,
                            SuspensionExpirationTime = ss.SuspensionExpirationTime,
                            Status = ss.SuspensionExpirationTime > DateTime.Now ? "停權中" : "已解除"
                        })
                        .OrderByDescending(s => s.SuspensionExpirationTime)
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "360度總覽頁出錯");
                return StatusCode(500, "系統錯誤");
            }
        }
    }
}
