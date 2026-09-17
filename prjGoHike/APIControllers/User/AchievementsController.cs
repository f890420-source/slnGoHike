using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.User;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.User;

[ApiController, Route("api/achievements"), Authorize]
public sealed class AchievementsController : ControllerBase
{
    private readonly GoHikeDataContext _context;
    public AchievementsController(GoHikeDataContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken ct) => Ok(await _context.Achievements.AsNoTracking().OrderBy(x => x.AchievementId).ToListAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult> Get(long id, CancellationToken ct)
    {
        var item = await _context.Achievements.AsNoTracking().FirstOrDefaultAsync(x => x.AchievementId == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(AchievementRequest request, CancellationToken ct)
    {
        if (await _context.Achievements.AnyAsync(x => x.Name == request.Name.Trim(), ct)) return Conflict(new { message = "成就名稱已存在。" });
        var item = new Achievement(); Apply(item, request); _context.Achievements.Add(item); await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = item.AchievementId }, item);
    }

    [HttpPut("{id:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(long id, AchievementRequest request, CancellationToken ct)
    {
        var item = await _context.Achievements.FindAsync([id], ct); if (item is null) return NotFound();
        if (await _context.Achievements.AnyAsync(x => x.AchievementId != id && x.Name == request.Name.Trim(), ct)) return Conflict(new { message = "成就名稱已存在。" });
        Apply(item, request); await _context.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("{id:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var item = await _context.Achievements.FindAsync([id], ct); if (item is null) return NotFound();
        if (await _context.UserAchievements.AnyAsync(x => x.AchievementId == id, ct)) return Conflict(new { message = "已有會員解鎖此成就，不能刪除。" });
        _context.Achievements.Remove(item); await _context.SaveChangesAsync(ct); return NoContent();
    }

    private static void Apply(Achievement item, AchievementRequest r)
    {
        item.Name = r.Name.Trim(); item.Description = r.Description.Trim(); item.Rarity = r.Rarity.Trim();
        item.ConditionType = r.ConditionType.Trim(); item.ConditionValue = r.ConditionValue.Trim();
    }
}
