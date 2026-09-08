using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.User;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.User;

[ApiController, Route("api/levels"), Authorize]
public sealed class LevelsController : ControllerBase
{
    private readonly GoHikeDataContext _context;
    public LevelsController(GoHikeDataContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken ct) =>
        Ok(await _context.Levels.AsNoTracking().OrderBy(x => x.MinXp).ToListAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult> Get(long id, CancellationToken ct)
    {
        var item = await _context.Levels.AsNoTracking().FirstOrDefaultAsync(x => x.LevelId == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(LevelRequest request, CancellationToken ct)
    {
        var error = await ValidateRange(request, 0, ct);
        if (error is not null) return Conflict(new { message = error });
        var item = new Level { LevelName = request.LevelName.Trim(), MinXp = request.MinXp, MaxXp = request.MaxXp };
        _context.Levels.Add(item);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = item.LevelId }, item);
    }

    [HttpPut("{id:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(long id, LevelRequest request, CancellationToken ct)
    {
        var item = await _context.Levels.FindAsync([id], ct);
        if (item is null) return NotFound();
        var error = await ValidateRange(request, id, ct);
        if (error is not null) return Conflict(new { message = error });
        item.LevelName = request.LevelName.Trim(); item.MinXp = request.MinXp; item.MaxXp = request.MaxXp;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var item = await _context.Levels.FindAsync([id], ct);
        if (item is null) return NotFound();
        if (await _context.Users.AnyAsync(u => u.CurrentLevelId == id, ct))
            return Conflict(new { message = "仍有會員使用此等級，不能刪除。" });
        _context.Levels.Remove(item); await _context.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPost("users/{userId:long}/xp"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> GrantXp(long userId, GrantXpRequest request, CancellationToken ct)
    {
        var user = await _context.Users.FindAsync([userId], ct);
        if (user is null) return NotFound(new { message = "找不到會員。" });
        try { user.TotalXp = checked(user.TotalXp + request.Amount); }
        catch (OverflowException) { return BadRequest(new { message = "經驗值超過可接受範圍。" }); }
        var level = await _context.Levels.FirstOrDefaultAsync(x => x.MinXp <= user.TotalXp && x.MaxXp >= user.TotalXp, ct);
        if (level is null) return Conflict(new { message = "目前沒有涵蓋此經驗值的等級設定。" });
        user.CurrentLevelId = level.LevelId; await _context.SaveChangesAsync(ct);
        return Ok(new { user.UserId, user.TotalXp, level.LevelId, level.LevelName });
    }

    private async Task<string?> ValidateRange(LevelRequest request, long excludedId, CancellationToken ct)
    {
        if (request.MinXp > request.MaxXp) return "最低經驗值不可大於最高經驗值。";
        if (await _context.Levels.AnyAsync(x => x.LevelId != excludedId && request.MinXp <= x.MaxXp && request.MaxXp >= x.MinXp, ct))
            return "經驗值範圍與其他等級重疊。";
        if (await _context.Levels.AnyAsync(x => x.LevelId != excludedId && x.LevelName == request.LevelName.Trim(), ct))
            return "等級名稱已存在。";
        return null;
    }
}
