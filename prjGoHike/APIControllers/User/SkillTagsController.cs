using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.User;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.User;

[ApiController, Route("api/skill-tags"), Authorize]
public sealed class SkillTagsController : ControllerBase
{
    private readonly GoHikeDataContext _context;
    public SkillTagsController(GoHikeDataContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken ct) => Ok(await _context.SkillTags.AsNoTracking().OrderBy(x => x.Category).ThenBy(x => x.TagName).ToListAsync(ct));

    [HttpGet("tree")]
    public async Task<ActionResult> GetTree(CancellationToken ct)
    {
        var nodes = await _context.SkillTags.AsNoTracking().Select(x => new SkillTagTreeNodeDto
        {
            TagId = x.TagId, Category = x.Category, TagName = x.TagName,
            ParentTagId = x.ParentTagId, UnlockCondition = x.UnlockCondition
        }).ToListAsync(ct);
        var byId = nodes.ToDictionary(x => x.TagId);
        foreach (var node in nodes.Where(x => x.ParentTagId.HasValue))
            if (byId.TryGetValue(node.ParentTagId!.Value, out var parent)) parent.Children.Add(node);
        return Ok(nodes.Where(x => !x.ParentTagId.HasValue || !byId.ContainsKey(x.ParentTagId.Value)));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult> Get(long id, CancellationToken ct)
    {
        var item = await _context.SkillTags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(SkillTagRequest request, CancellationToken ct)
    {
        var error = await Validate(request, 0, ct); if (error is not null) return Conflict(new { message = error });
        var item = new SkillTag(); Apply(item, request); _context.SkillTags.Add(item); await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = item.TagId }, item);
    }

    [HttpPut("{id:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(long id, SkillTagRequest request, CancellationToken ct)
    {
        var item = await _context.SkillTags.FindAsync([id], ct); if (item is null) return NotFound();
        var error = await Validate(request, id, ct); if (error is not null) return Conflict(new { message = error });
        Apply(item, request); await _context.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("{id:long}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var item = await _context.SkillTags.FindAsync([id], ct); if (item is null) return NotFound();
        if (await _context.SkillTags.AnyAsync(x => x.ParentTagId == id, ct) || await _context.UserSkillTags.AnyAsync(x => x.TagId == id, ct))
            return Conflict(new { message = "此標籤仍有子標籤或會員關聯，不能刪除。" });
        _context.SkillTags.Remove(item); await _context.SaveChangesAsync(ct); return NoContent();
    }

    private async Task<string?> Validate(SkillTagRequest request, long id, CancellationToken ct)
    {
        if (await _context.SkillTags.AnyAsync(x => x.TagId != id && x.Category == request.Category.Trim() && x.TagName == request.TagName.Trim(), ct))
            return "同分類下的標籤名稱已存在。";
        if (!request.ParentTagId.HasValue) return null;
        if (request.ParentTagId == id) return "標籤不能以自己作為父標籤。";
        var parentId = request.ParentTagId;
        var visited = new HashSet<long>();
        while (parentId.HasValue)
        {
            if (!visited.Add(parentId.Value) || parentId == id) return "父標籤設定會形成循環。";
            var parent = await _context.SkillTags.AsNoTracking().Where(x => x.TagId == parentId).Select(x => new { x.ParentTagId }).FirstOrDefaultAsync(ct);
            if (parent is null) return "找不到父標籤。";
            parentId = parent.ParentTagId;
        }
        return null;
    }

    private static void Apply(SkillTag item, SkillTagRequest r)
    {
        item.Category = r.Category.Trim(); item.TagName = r.TagName.Trim(); item.ParentTagId = r.ParentTagId;
        item.UnlockCondition = r.UnlockCondition?.Trim() ?? string.Empty;
    }
}
