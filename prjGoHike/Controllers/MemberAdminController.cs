using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.ViewModels_user.Member;
using static prjGoHike.Models.UserPermissions;
using static prjGoHike.ViewModels_user.Member.MemberAdminViewModel;

namespace prjGoHike.Controllers
{
    public class MemberAdminController : Controller
    {
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateMemberViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateMemberViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = new Member
            {
                Nickname = vm.Nickname,
                Email = vm.Email,
                PasswordHash = vm.PasswordHash,  // ⚠️ 實務上要加密
                Role = "一般會員",
                AccountStatus = "正常",
                CurrentLevelId = 1,  // 新人預設等級 1
                TotalXp = 0,
                RegionPreference = vm.RegionPreference ?? "",
                DifficultyPreference = vm.DifficultyPreference ?? "",
                CreatedAt = DateTime.Now,
                LastActiveAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "會員新增成功";
            return RedirectToAction(nameof(MemberList));
        }

        private readonly GoHikeDataContext _context;
            public MemberAdminController(GoHikeDataContext context)
            {
                _context = context;
            }

            // 會員列表(搜尋 + 角色篩選 + 分頁)
            public async Task<IActionResult> MemberList(string? search, string? role, int page = 1, int pageSize = 20)
            {
                var query = _context.Users.AsQueryable(); // Users 為 TPH 基底 DbSet

                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(u => u.Nickname.Contains(search) || u.Email.Contains(search));

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role);

            var total = await query.CountAsync();

                var items = await query
                    .OrderBy(u => u.UserId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new MemberListItemViewModel
                    {
                        Id = u.UserId,
                        Name = u.Nickname,
                        Email = u.Email,
                        Role = EF.Property<string>(u, "Discriminator"),
                        IsSuspended = u.SuspensionSchedules.Any(s => s.SuspensionExpirationTime > DateTime.Now)
                    })
                    .ToListAsync();

                return View(new MemberListViewModel
                {
                    Items = items,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Search = search,
                    RoleFilter = role
                });
            }

        // 360度總覽(針對指定會員,非目前登入者)
        //	管理員看指定會員的資料(需要傳 ID)
        public async Task<IActionResult> Dashboard(long id)
            {
            var vm = await BuildDashboardViewModel(id);
            if (vm == null)
                return NotFound("會員不存在");

            return View(vm);
        }
        /// <summary>
        /// 共用方法:建構會員 Dashboard ViewModel
        /// </summary>
        private async Task<MemberDashboardViewModel> BuildDashboardViewModel(long userId)
        {
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
                return null;

            var totalAchievements = await _context.Achievements.CountAsync();

            return new MemberDashboardViewModel
            {
                Nickname = user.Nickname,
                AvatarUrl = user.AvatarUrl,
                AccountStatus = user.AccountStatus,

                // 等級資訊(計算屬性會自動算 ProgressPercentage)
                LevelName = user.CurrentLevel?.LevelName ?? "未設定",
                TotalXp = user.TotalXp,
                MinXp = user.CurrentLevel?.MinXp ?? 0,
                MaxXp = user.CurrentLevel?.MaxXp ?? 1000,

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

                // 爬山紀錄
                HikeRecords = user.HikeRecords
                    .Select(hr => new HikeRecordDto
                    {
                        RecordId = hr.RecordId,
                        MountainId = hr.MountainId,
                        MountainName = "待補(另一組模組)",
                        HikeDate = hr.HikeDate,
                        CompanionCount = hr.CompanionCount,
                        Verified = hr.Verified
                    })
                    .OrderByDescending(h => h.HikeDate)
                    .ToList(),
                TotalHikeCount = user.HikeRecords.Count,

                // 停權紀錄
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
        }
        // 角色調整 - 顯示表單
        [HttpGet]
            public async Task<IActionResult> ChangeRole(long id)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound();

                var currentRole = _context.Entry(user)
                    .Property("Discriminator").CurrentValue?.ToString();

                return View(new ChangeRoleViewModel
                {
                    UserId = user.UserId,
                    Name = user.Nickname,
                    CurrentRole = currentRole
                });
            }

            // 角色調整 - 送出
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ChangeRole(long id, string newRole)
            {
                var Roles = new[] { "一般會員", "團主", "管理員" };
                if (!Roles.Contains(newRole))
                {
                    ModelState.AddModelError("", "無效的角色");
                    return RedirectToAction(nameof(ChangeRole), new { id });
                }
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = newRole;  // ← 直接改 Role 欄位即可
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MemberList));
        }
        }
    }

