using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.GoHikeSafe;

[ApiController]
[Route("api/trailfeatures")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public class TrailFeaturesApiController : BaseController
{
    private readonly GoHikeDataContext _context;
    private readonly ILogger<TrailFeaturesApiController> _logger;

    public TrailFeaturesApiController(GoHikeDataContext context, ILogger<TrailFeaturesApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static TrailFeatureDto ToDto(TrailFeature item) => new()
    {
        FeatureId = item.FeatureId,
        TrailId = item.TrailId,
        FeatureType = item.FeatureType,
        FeatureName = item.FeatureName,
        Location = item.Location,
        FeatureDescription = item.FeatureDescription,
        ReliabilityLevel = item.ReliabilityLevel,
        IsAvailable = item.IsAvailable,
        DataSource = item.DataSource
    };

    private IQueryable<TrailFeature> QueryFeatures(TrailFeatureQueryDto query, bool publicOnly)
    {
        var items = _context.TrailFeatures.AsNoTracking();
        if (publicOnly)
            items = items.Where(x => x.IsAvailable && x.Trail.IsPublished);
        if (query.TrailId.HasValue)
            items = items.Where(x => x.TrailId == query.TrailId.Value);
        if (!string.IsNullOrEmpty(query.FeatureType))
            items = items.Where(x => x.FeatureType == query.FeatureType);
        return items.OrderBy(x => x.FeatureId);
    }

    private static bool HasValidReport(TrailFeatureReportRequestDto payload) =>
        !string.IsNullOrWhiteSpace(payload.FeatureName)
        && payload.Location is Point { IsEmpty: false, SRID: 4326 };

    private static void ApplyReport(TrailFeature item, TrailFeatureReportRequestDto payload)
    {
        item.TrailId = payload.TrailId;
        item.FeatureType = payload.FeatureType.Trim();
        item.FeatureName = payload.FeatureName.Trim();
        item.Location = payload.Location;
        item.FeatureDescription = payload.FeatureDescription?.Trim();
    }

    [HttpGet]
    [EndpointSummary("取得已發布步道的可用特徵")]
    [EndpointDescription("可依 trailId、featureType 篩選，依 featureId 升冪排列；無符合資料時回傳空陣列。位置使用 GeoJSON。")]
    [ProducesResponseType(typeof(ApiResponse<List<TrailFeatureDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] TrailFeatureQueryDto query, CancellationToken cancellationToken)
    {
        try
        {
            var items = await QueryFeatures(query, publicOnly: true).ToListAsync(cancellationToken);
            return SuccessResponse(items.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢步道特徵失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("取得指定可用步道特徵")]
    [ProducesResponseType(typeof(ApiResponse<TrailFeatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("特徵 ID 必須大於 0。");
        try
        {
            var item = await _context.TrailFeatures.AsNoTracking()
                .FirstOrDefaultAsync(x => x.FeatureId == id && x.IsAvailable && x.Trail.IsPublished, cancellationToken);
            return item is null ? NotFoundResponse() : SuccessResponse(ToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢步道特徵失敗 (Id: {Id})", id);
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("取得所有步道特徵回報管理資料")]
    [EndpointDescription("包含不可用及未發布步道的特徵，可依 trailId、featureType、isAvailable 篩選。")]
    [ProducesResponseType(typeof(ApiResponse<List<TrailFeatureDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminList([FromQuery] TrailFeatureAdminQueryDto query, CancellationToken cancellationToken)
    {
        try
        {
            var itemsQuery = QueryFeatures(query, publicOnly: false);
            if (query.IsAvailable.HasValue)
                itemsQuery = itemsQuery.Where(x => x.IsAvailable == query.IsAvailable.Value);
            var items = await itemsQuery.ToListAsync(cancellationToken);
            return SuccessResponse(items.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢步道特徵管理資料失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("admin/{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("取得指定步道特徵回報管理資料")]
    [ProducesResponseType(typeof(ApiResponse<TrailFeatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminDetail(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("特徵 ID 必須大於 0。");
        try
        {
            var item = await _context.TrailFeatures.AsNoTracking()
                .FirstOrDefaultAsync(x => x.FeatureId == id, cancellationToken);
            return item is null ? NotFoundResponse() : SuccessResponse(ToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢步道特徵管理資料失敗 (Id: {Id})", id);
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    [Authorize]
    [EndpointSummary("回報步道周圍發現的特徵")]
    [EndpointDescription("登入者可回報已發布步道；location 限有效 GeoJSON Point。伺服器固定初始可信度為 1、不可用，來源為 Member report；由管理員確認後開放。")]
    [ProducesResponseType(typeof(ApiResponse<TrailFeatureDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(TrailFeatureReportRequestDto payload, CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
            return Unauthorized();
        if (!ModelState.IsValid || !HasValidReport(payload))
            return ErrorResponse("請檢查回報欄位，位置必須是有效的 GeoJSON Point。");
        try
        {
            if (!await _context.Trails.AnyAsync(x => x.TrailId == payload.TrailId && x.IsPublished, cancellationToken))
                return NotFoundResponse("找不到已發布的步道。");
            var item = new TrailFeature
            {
                ReliabilityLevel = 1,
                IsAvailable = false,
                DataSource = "Member report"
            };
            ApplyReport(item, payload);
            _context.TrailFeatures.Add(item);
            await _context.SaveChangesAsync(cancellationToken);
            return CreatedResponse(ToDto(item), "回報成功，待管理員確認。");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            _logger.LogError(ex, "回報步道特徵時步道關聯已變更");
            return ErrorResponse("步道資料已變更，請重新查詢後回報。", statusCode: StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回報步道特徵失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("更新步道特徵回報")]
    [EndpointDescription("僅限管理員。完整替換可編輯欄位；可信度介於 1～5，isAvailable 控制是否可公開，目標步道可為未發布步道。")]
    [ProducesResponseType(typeof(ApiResponse<TrailFeatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(long id, TrailFeatureUpdateRequestDto payload, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("特徵 ID 必須大於 0。");
        if (!ModelState.IsValid || !HasValidReport(payload) || !payload.IsAvailable.HasValue)
            return ErrorResponse("請檢查回報欄位，位置必須是有效的 GeoJSON Point。");
        try
        {
            var item = await _context.TrailFeatures.FirstOrDefaultAsync(x => x.FeatureId == id, cancellationToken);
            if (item is null) return NotFoundResponse();
            if (!await _context.Trails.AnyAsync(x => x.TrailId == payload.TrailId, cancellationToken))
                return NotFoundResponse("找不到指定的步道。");
            ApplyReport(item, payload);
            item.ReliabilityLevel = payload.ReliabilityLevel;
            item.IsAvailable = payload.IsAvailable.Value;
            item.DataSource = payload.DataSource?.Trim();
            await _context.SaveChangesAsync(cancellationToken);
            return SuccessResponse(ToDto(item));
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            _logger.LogError(ex, "更新步道特徵時步道關聯已變更 (Id: {Id})", id);
            return ErrorResponse("步道資料已變更，請重新查詢後更新。", statusCode: StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新步道特徵失敗 (Id: {Id})", id);
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("刪除步道特徵回報")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("特徵 ID 必須大於 0。");
        try
        {
            var item = await _context.TrailFeatures.FirstOrDefaultAsync(x => x.FeatureId == id, cancellationToken);
            if (item is null) return NotFoundResponse();
            _context.TrailFeatures.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
            return SuccessResponse<string>("", message: "刪除資料成功！");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            _logger.LogError(ex, "刪除步道特徵失敗 (Id: {Id})", id);
            return ErrorResponse("資料目前無法刪除，請確認關聯資料。", statusCode: StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除步道特徵失敗 (Id: {Id})", id);
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
