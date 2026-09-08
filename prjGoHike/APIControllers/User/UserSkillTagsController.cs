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

    private async Task<ActionResult> Add(long userId, long tagId, string source, CancellationToken ct)
    {
        if (!await _context.Users.AnyAsync(x => x.UserId == userId, ct) || !await _context.SkillTags.AnyAsync(x => x.TagId == tagId, ct))
            return NotFound();
        if (await _context.UserSkillTags.AnyAsync(x => x.UserId == userId && x.TagId == tagId, ct))
            return Conflict(new { message = "會員已擁有此標籤。" });
        var item = new UserSkillTag { UserId = userId, TagId = tagId, Source = source };
        _context.UserSkillTags.Add(item); await _context.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    private async Task<ActionResult> Remove(long userId, long tagId, CancellationToken ct)
    {
        var item = await _context.UserSkillTags.FindAsync([userId, tagId], ct);
        if (item is null) return NotFound();
        _context.UserSkillTags.Remove(item); await _context.SaveChangesAsync(ct); return NoContent();
    }

    private IQueryable<UserSkillTagDto> Query(long userId) => _context.UserSkillTags.AsNoTracking()
        .Where(x => x.UserId == userId).OrderBy(x => x.SkillTag.Category).ThenBy(x => x.SkillTag.TagName)
        .Select(x => new UserSkillTagDto
        {
            UserId = x.UserId, TagId = x.TagId, Source = x.Source,
            Category = x.SkillTag.Category, TagName = x.SkillTag.TagName,
            ParentTagId = x.SkillTag.ParentTagId, UnlockCondition = x.SkillTag.UnlockCondition
        });
}
