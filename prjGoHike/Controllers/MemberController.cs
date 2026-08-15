using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Data;
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

            }
    }
}
