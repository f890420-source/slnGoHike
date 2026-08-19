
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.ViewModels_user.Member;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static prjGoHike.Models.UserPermissions;

namespace GoHike.Controllers
{
    public class LoginController : Controller
    {
        private readonly GoHikeDataContext _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(GoHikeDataContext context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region 登入相關

        /// <summary>
        /// 顯示登入表單
        /// </summary>
        [HttpGet]
        [Route("Login")]
        [Route("Login/Index")]
        public IActionResult Index()
        {
            // 如果已登入，導向首頁
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        /// <summary>
        /// 處理登入（POST）
        /// </summary>
        [HttpPost]
        [Route("Login")]
        [Route("Login/Index")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ErrorMessage = "請檢查輸入的資料";
                return View("Index", model);
            }

            try
            {
                // 根據信箱查詢使用者
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    model.ErrorMessage = "信箱或密碼錯誤";
                    _logger.LogWarning($"登入失敗：信箱 {model.Email} 不存在");
                    return View("Index", model);
                }
            
                // 驗證帳戶狀態
                if (user.AccountStatus != "正常")
                {
                    model.ErrorMessage = $"帳戶已被停用，狀態：{user.AccountStatus}";
                    _logger.LogWarning($"登入失敗：使用者 {user.UserId} 帳戶狀態異常");
                    return View("Index", model);
                }

                // 檢查停權
                var activeSuspension = await _context.SuspensionSchedules
                    .FirstOrDefaultAsync(s =>
                        s.UserId == user.UserId &&
                        s.SuspensionExpirationTime > DateTime.Now);

                if (activeSuspension != null)
                {
                    model.ErrorMessage = $"帳戶已被停權至 {activeSuspension.SuspensionExpirationTime:yyyy-MM-dd}，原因：{activeSuspension.Reason}";
                    _logger.LogWarning($"登入失敗：使用者 {user.UserId} 處於停權狀態");
                    return View("Index", model);
                }

                // 驗證密碼
                if (!VerifyPassword(model.Password, user.PasswordHash))
                {
                    model.ErrorMessage = "信箱或密碼錯誤";
                    _logger.LogWarning($"登入失敗：使用者 {user.UserId} 密碼驗證失敗");
                    return View("Index", model);
                }

                // 登入成功 - 建立 Cookie
                await SignInUser(user, model.RememberMe);

                _logger.LogInformation($"使用者 ({user.Nickname}) 登入成功");

                // 重定向到登入前的頁面，或首頁
                var returnUrl = Request.Query["returnUrl"].ToString();
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Dashboard", "Member");
            }
            catch (Exception ex)
            {
                _logger.LogError($"登入過程發生錯誤：{ex.Message}");
                model.ErrorMessage = "登入過程發生錯誤，請稍後再試";
                return View("Index", model);
            }
        }

        /// <summary>
        /// 登出
        /// </summary>
        [HttpGet]
        [Route("Login/Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"使用者已登出");
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region 註冊相關

        /// <summary>
        /// 顯示註冊表單
        /// </summary>
        [HttpGet]
        [Route("Register")]
        [Route("Register/Index")]
        public IActionResult RegisterIndex()
        {
            // 如果已登入，導向首頁
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        /// <summary>
        /// 處理註冊（POST）
        /// </summary>
        [HttpPost]
        [Route("Register")]
        [Route("Register/Index")]
        public async Task<IActionResult> RegisterIndex(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ErrorMessage = "請檢查輸入的資料";
                return View(model);
            }

            try
            {
                // 檢查信箱是否已被註冊
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    model.ErrorMessage = "此信箱已被註冊，請使用其他信箱";
                    _logger.LogWarning($"註冊失敗：信箱 {model.Email} 已存在");
                    return View(model);
                }

                // 檢查暱稱是否重複
                var existingNickname = await _context.Users
                    .FirstOrDefaultAsync(u => u.Nickname == model.Nickname);

                if (existingNickname != null)
                {
                    model.ErrorMessage = "此暱稱已被使用，請更換";
                    _logger.LogWarning($"註冊失敗：暱稱 {model.Nickname} 已存在");
                    return View(model);
                }

                // ✅ 建立新會員
                var newMember = new Member
                {
                    Nickname = model.Nickname,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),  // 密碼加密
                    Role = "一般會員",
                    AccountStatus = "正常",
                    CurrentLevelId = 1,  // ⚠️ 確保 levels 表中 ID=1 存在
                    TotalXp = 0,
                    RegionPreference = model.RegionPreference ?? "",
                    DifficultyPreference = model.DifficultyPreference ?? "",
                    AvatarUrl = "",
                    AvatarBlurState = "不模糊",
                    Bio = "",
                    CreatedAt = DateTime.Now,
                    LastActiveAt = DateTime.Now
                };

                _context.Users.Add(newMember);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"新會員註冊成功：{newMember.Nickname} ({newMember.UserId})");

                
                TempData["SuccessMessage"] = $"註冊成功！請使用新帳號密碼進行登入。";

               
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                //_logger.LogError($"註冊過程發生錯誤：{ex.Message}");
                //model.ErrorMessage = "註冊過程發生錯誤，請稍後再試";
                //return View(model);
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                model.ErrorMessage = $"註冊失敗：{ex.Message} | 內層細節：{innerMsg}";
                return View(model);
            }
        }

        #endregion

        #region 輔助方法

        /// <summary>
        /// 密碼加密（使用 SHA256）
        /// ⚠️ 實務上建議改用 BCrypt 或 Argon2，見下方註解
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }

            // 🔐 更安全的做法（需先安裝 NuGet: BCrypt.Net-Next）：
            // return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// 密碼驗證
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashOfInput = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashOfInputAsString = Convert.ToBase64String(hashOfInput);
                return hashOfInputAsString == hash;
            }

            // 🔐 BCrypt 驗證方式：
            // return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        /// <summary>
        /// 建立認證 Cookie 並登入使用者
        /// </summary>
        private async Task SignInUser(User user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Nickname),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Role", user.Role),
                new Claim("AccountStatus", user.AccountStatus)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)  // 記住我 30 天
                    : DateTimeOffset.UtcNow.AddHours(8)  // 否則 8 小時
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 更新最後活動時間
            user.LastActiveAt = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
