using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.User;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.User;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : UserApiControllerBase
{
    private const long MaxAvatarBytes = 5 * 1024 * 1024;
    private readonly GoHikeDataContext _context;
    private readonly IWebHostEnvironment _environment;

    public UsersController(GoHikeDataContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var user = await UserDetailsQuery().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var nickname = request.Nickname.Trim();
        if (await _context.Users.AnyAsync(u => u.UserId != userId && u.Nickname == nickname, cancellationToken))
            return Conflict(new { message = "暱稱已被使用。" });

        if (request.DisplayedAchievementId is long achievementId &&
            !await _context.UserAchievements.AnyAsync(
                x => x.UserId == userId && x.AchievementId == achievementId,
                cancellationToken))
        {
            return BadRequest(new { message = "展示成就必須是你已解鎖的成就。" });
        }

        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user is null) return NotFound();

        user.Nickname = nickname;
        user.Bio = request.Bio?.Trim();
        user.AvatarBlurState = request.AvatarBlurState?.Trim();
        user.RegionPreference = request.RegionPreference?.Trim() ?? string.Empty;
        user.DifficultyPreference = request.DifficultyPreference?.Trim() ?? string.Empty;
        user.DisplayedAchievementId = request.DisplayedAchievementId;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("me/avatar-blur")]
    public async Task<ActionResult> SetAvatarBlur(SetAvatarBlurRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user is null) return NotFound();

        user.AvatarBlurState = request.IsBlurred ? "模糊" : "不模糊";
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(MaxAvatarBytes + 64 * 1024)]
    public async Task<ActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        if (file.Length == 0) return BadRequest(new { message = "請選擇圖片檔案。" });
        if (file.Length > MaxAvatarBytes) return BadRequest(new { message = "頭像大小不可超過 5 MB。" });

        var extension = file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null
        };
        if (extension is null)
            return BadRequest(new { message = "頭像僅支援 JPG、PNG 或 WebP。" });

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (!HasValidImageSignature(bytes, extension))
            return BadRequest(new { message = "圖片內容與檔案格式不符。" });

        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user is null) return NotFound();

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var avatarDirectory = Path.Combine(webRoot, "uploads", "avatars");
        Directory.CreateDirectory(avatarDirectory);

        var fileName = $"{userId}_{Guid.NewGuid():N}{extension}";
        await System.IO.File.WriteAllBytesAsync(
            Path.Combine(avatarDirectory, fileName), bytes, cancellationToken);

        var previousAvatarUrl = user.AvatarUrl;
        user.AvatarUrl = $"/uploads/avatars/{fileName}";
        await _context.SaveChangesAsync(cancellationToken);
        DeletePreviousLocalAvatar(previousAvatarUrl, avatarDirectory);

        return Ok(new { avatarUrl = user.AvatarUrl });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = UserDetailsQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(u => u.Nickname.Contains(keyword) || u.Email.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role.Trim());

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(u => u.UserId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var user = await UserDetailsQuery().FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPatch("{id:long}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ChangeRole(long id, ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        var role = NormalizeRole(request.Role);
        if (role is null) return BadRequest(new { message = "角色僅能是 Member、EventLeader 或 Admin。" });
        if (!await _context.Users.AnyAsync(u => u.UserId == id, cancellationToken)) return NotFound();

        // Role 同時是 TPH discriminator，使用直接更新避免 EF Core 禁止修改 discriminator。
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE users SET role = {role} WHERE user_id = {id}", cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ChangeStatus(long id, ChangeAccountStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        user.AccountStatus = request.AccountStatus.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<UserSummary> UserDetailsQuery() => _context.Users.AsNoTracking().Select(u => new UserSummary
    {
        UserId = u.UserId,
        Role = u.Role,
        Nickname = u.Nickname,
        Email = u.Email,
        AccountStatus = u.AccountStatus,
        AvatarUrl = u.AvatarUrl,
        AvatarBlurState = u.AvatarBlurState,
        Bio = u.Bio,
        TotalXp = u.TotalXp,
        CurrentLevelId = u.CurrentLevelId,
        CurrentLevelName = u.CurrentLevel.LevelName,
        RegionPreference = u.RegionPreference,
        DifficultyPreference = u.DifficultyPreference,
        CreatedAt = u.CreatedAt,
        LastActiveAt = u.LastActiveAt,
        AchievementCount = u.UserAchievements.Count,
        SkillTagCount = u.UserSkillTags.Count,
        DisplayedAchievementId = u.DisplayedAchievementId,
        DisplayedAchievementName = u.DisplayedAchievement == null ? null : u.DisplayedAchievement.Name,
        DisplayedAchievementRarity = u.DisplayedAchievement == null ? null : u.DisplayedAchievement.Rarity
    });

    private static bool HasValidImageSignature(byte[] bytes, string extension) => extension switch
    {
        ".jpg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        ".png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ".webp" => bytes.Length >= 12 &&
            bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    private static void DeletePreviousLocalAvatar(string? avatarUrl, string avatarDirectory)
    {
        const string prefix = "/uploads/avatars/";
        if (string.IsNullOrWhiteSpace(avatarUrl) || !avatarUrl.StartsWith(prefix, StringComparison.Ordinal)) return;

        var previousFileName = Path.GetFileName(avatarUrl);
        var previousPath = Path.Combine(avatarDirectory, previousFileName);
        try
        {
            if (System.IO.File.Exists(previousPath)) System.IO.File.Delete(previousPath);
        }
        catch (IOException)
        {
            // 新頭像與資料庫已更新成功；舊檔清理失敗不應讓 API 回傳 500。
        }
    }

    private static string? NormalizeRole(string role) => role.Trim() switch
    {
        "Member" or "一般會員" => "一般會員",
        "EventLeader" or "團主" => "團主",
        "Admin" or "管理員" => "管理員",
        _ => null
    };

    private sealed class UserSummary
    {
        public long UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? AvatarBlurState { get; set; }
        public string? Bio { get; set; }
        public int TotalXp { get; set; }
        public long CurrentLevelId { get; set; }
        public string CurrentLevelName { get; set; } = string.Empty;
        public string RegionPreference { get; set; } = string.Empty;
        public string DifficultyPreference { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastActiveAt { get; set; }
        public int AchievementCount { get; set; }
        public int SkillTagCount { get; set; }
        public long? DisplayedAchievementId { get; set; }
        public string? DisplayedAchievementName { get; set; }
        public string? DisplayedAchievementRarity { get; set; }
    }
}
