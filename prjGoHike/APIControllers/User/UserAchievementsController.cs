using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.User;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.User;

[ApiController, Route("api/user-achievements"), Authorize]
public sealed class UserAchievementsController : UserApiControllerBase
{
    private readonly GoHikeDataContext _context;
    public UserAchievementsController(GoHikeDataContext context) => _context = context;

    [HttpGet("me")]
    public async Task<ActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return Ok(await Query(userId).ToListAsync(ct));
    }

    [HttpGet("users/{userId:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetForUser(long userId, CancellationToken ct)
    {
        if (!await _context.Users.AnyAsync(x => x.UserId == userId, ct)) return NotFound();
        return Ok(await Query(userId).ToListAsync(ct));
    }

    [HttpPost("users/{userId:long}/achievements/{achievementId:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> Unlock(long userId, long achievementId, CancellationToken ct)
    {
        if (!await _context.Users.AnyAsync(x => x.UserId == userId, ct) ||
            !await _context.Achievements.AnyAsync(x => x.AchievementId == achievementId, ct)) return NotFound();
        if (await _context.UserAchievements.AnyAsync(x => x.UserId == userId && x.AchievementId == achievementId, ct))
            return Conflict(new { message = "會員已解鎖此成就。" });
        var item = new UserAchievement { UserId = userId, AchievementId = achievementId, UnlockedAt = DateTime.UtcNow };
        _context.UserAchievements.Add(item); await _context.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    [HttpDelete("users/{userId:long}/achievements/{achievementId:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> Remove(long userId, long achievementId, CancellationToken ct)
    {
        var item = await _context.UserAchievements.FindAsync([userId, achievementId], ct);
        if (item is null) return NotFound();
        _context.UserAchievements.Remove(item); await _context.SaveChangesAsync(ct); return NoContent();
    }

    private IQueryable<UserAchievementDto> Query(long userId) => _context.UserAchievements.AsNoTracking()
        .Where(x => x.UserId == userId).OrderByDescending(x => x.UnlockedAt)
        .Select(x => new UserAchievementDto
        {
            UserId = x.UserId, AchievementId = x.AchievementId, UnlockedAt = x.UnlockedAt,
            Name = x.Achievement.Name, Description = x.Achievement.Description,
            Rarity = x.Achievement.Rarity, ConditionType = x.Achievement.ConditionType,
            ConditionValue = x.Achievement.ConditionValue
        });
}
