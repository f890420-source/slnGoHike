using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.User;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.User;

[ApiController, Route("api/user-skill-tags"), Authorize]
public sealed class UserSkillTagsController : UserApiControllerBase
{
    private readonly GoHikeDataContext _context;
    public UserSkillTagsController(GoHikeDataContext context) => _context = context;

    [HttpGet("me")]
    public async Task<ActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return Ok(await Query(userId).ToListAsync(ct));
    }

    [HttpPost("me/tags/{tagId:long}")]
    public async Task<ActionResult> AddMine(long tagId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await Add(userId, tagId, "自我設定", ct);
    }

    [HttpDelete("me/tags/{tagId:long}")]
    public async Task<ActionResult> RemoveMine(long tagId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await Remove(userId, tagId, ct);
    }

    [HttpGet("users/{userId:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetForUser(long userId, CancellationToken ct)
    {
        if (!await _context.Users.AnyAsync(x => x.UserId == userId, ct)) return NotFound();
        return Ok(await Query(userId).ToListAsync(ct));
    }

    [HttpPost("users/{userId:long}/tags/{tagId:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> AddForUser(long userId, long tagId, AssignSkillTagRequest request, CancellationToken ct) =>
        await Add(userId, tagId, string.IsNullOrWhiteSpace(request.Source) ? "管理員" : request.Source.Trim(), ct);

    [HttpDelete("users/{userId:long}/tags/{tagId:long}"), Authorize(Roles = "Admin")]
    public Task<ActionResult> RemoveForUser(long userId, long tagId, CancellationToken ct) => Remove(userId, tagId, ct);

    [HttpPatch("me/tags/{tagId:long}/display")]
    public async Task<ActionResult> SetDisplayed(long tagId, SetSkillTagDisplayRequest request, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var item = await _context.UserSkillTags.FindAsync([userId, tagId], ct);
        if (item is null) return NotFound(new { message = "你尚未解鎖此技能。" });

        if (request.IsDisplayed && !item.IsDisplayed &&
            await _context.UserSkillTags.CountAsync(x => x.UserId == userId && x.IsDisplayed, ct) >= 3)
            return BadRequest(new { message = "最多只能展示三個技能。" });

        item.IsDisplayed = request.IsDisplayed;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<ActionResult> Add(long userId, long tagId, string source, CancellationToken ct)
    {
        var user = await _context.Users.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.TotalXp })
            .FirstOrDefaultAsync(ct);
        if (user is null) return NotFound();

        var tag = await _context.SkillTags.AsNoTracking()
            .Where(x => x.TagId == tagId)
            .Select(x => new { x.ParentTagId })
            .FirstOrDefaultAsync(ct);
        if (tag is null) return NotFound();

        if (tag.ParentTagId.HasValue && !await _context.UserSkillTags
                .AnyAsync(x => x.UserId == userId && x.TagId == tag.ParentTagId.Value, ct))
            return Conflict(new { message = "請先解鎖前置技能。" });

        if (await _context.UserSkillTags.AnyAsync(x => x.UserId == userId && x.TagId == tagId, ct))
            return Conflict(new { message = "會員已擁有此標籤。" });

        var reachedLevelCount = await _context.Levels.CountAsync(x => x.MinXp <= user.TotalXp, ct);
        var earnedSkillPoints = Math.Max(0, reachedLevelCount - 1);
        var usedSkillPoints = await _context.UserSkillTags.CountAsync(x => x.UserId == userId, ct);
        if (usedSkillPoints >= earnedSkillPoints)
            return Conflict(new { message = "技能點數不足；每提升一級可獲得 1 點。" });

        var item = new UserSkillTag { UserId = userId, TagId = tagId, Source = source };
        _context.UserSkillTags.Add(item); await _context.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    private async Task<ActionResult> Remove(long userId, long tagId, CancellationToken ct)
    {
        var item = await _context.UserSkillTags.FindAsync([userId, tagId], ct);
        if (item is null) return NotFound();

        var hasUnlockedChild = await _context.UserSkillTags.AnyAsync(
            x => x.UserId == userId && x.SkillTag.ParentTagId == tagId, ct);
        if (hasUnlockedChild)
            return Conflict(new { message = "請先移除後續技能。" });

        _context.UserSkillTags.Remove(item); await _context.SaveChangesAsync(ct); return NoContent();
    }

    private IQueryable<UserSkillTagDto> Query(long userId) => _context.UserSkillTags.AsNoTracking()
        .Where(x => x.UserId == userId).OrderBy(x => x.SkillTag.Category).ThenBy(x => x.SkillTag.TagName)
        .Select(x => new UserSkillTagDto
        {
            UserId = x.UserId, TagId = x.TagId, Source = x.Source,
            Category = x.SkillTag.Category, TagName = x.SkillTag.TagName,
            ParentTagId = x.SkillTag.ParentTagId, UnlockCondition = x.SkillTag.UnlockCondition,
            IsDisplayed = x.IsDisplayed
        });
}
