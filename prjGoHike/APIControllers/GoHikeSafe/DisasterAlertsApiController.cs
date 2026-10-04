using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.GoHikeSafe;

[ApiController]
[Route("api/disasteralerts")]
public class DisasterAlertsApiController : BaseController
{
    private readonly GoHikeDataContext _context;
    private readonly ILogger<DisasterAlertsApiController> _logger;

    public DisasterAlertsApiController(GoHikeDataContext context, ILogger<DisasterAlertsApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static DisAlertDto ToDto(DisasterAlert item) => new()
    {
        AlertId = item.AlertId,
        AlertType = item.AlertType,
        AlertTitle = item.AlertTitle,
        AlertDescription = item.AlertDescription,
        SeverityLevel = item.SeverityLevel,
        EffectiveFrom = item.EffectiveFrom,
        EffectiveTo = item.EffectiveTo,
        SourceAgency = item.SourceAgency,
        SourceUrl = item.SourceUrl,
        IsActive = item.IsActive,
        AlertSegments = item.AlertSegments.Select(s => new AlertSegmentDto
        {
            id = s.AlertSegmentId,
            SegmentName = s.SegmentName,
            Shape = s.Shape,
            SourceFeatureId = s.SourceFeatureId,
            SegmentLevel = s.SegmentLevel,
            Description = s.Description,
        }).ToList()
    };

    private static bool HasValidSegments(IEnumerable<AlertSegmentDto>? segments) =>
        segments is null || segments.All(segment => segment is not null && segment.Shape is not null);

    [HttpGet]
    [EndpointSummary("取得所有啟用的災害警示")]
    [ProducesResponseType(typeof(ApiResponse<List<DisAlertDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _context.DisasterAlerts.AsNoTracking().Include(x => x.AlertSegments)
                .Where(x => x.IsActive).OrderBy(x => x.AlertId).ToListAsync(cancellationToken);
            return SuccessResponse(items.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢災害警示失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("取得指定啟用的災害警示")]
    [ProducesResponseType(typeof(ApiResponse<DisAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("災害警示 ID 必須大於 0。");
        try
        {
            var item = await _context.DisasterAlerts.AsNoTracking().Include(x => x.AlertSegments)
                .FirstOrDefaultAsync(x => x.IsActive && x.AlertId == id, cancellationToken);
            return item is null ? NotFoundResponse() : SuccessResponse(ToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢災害警示失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("取得所有災害警示管理資料")]
    [ProducesResponseType(typeof(ApiResponse<List<DisAlertDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminList(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _context.DisasterAlerts.AsNoTracking().Include(x => x.AlertSegments)
                .OrderBy(x => x.AlertId).ToListAsync(cancellationToken);
            return SuccessResponse(items.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢災害警示失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("admin/{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("取得指定災害警示管理資料")]
    [ProducesResponseType(typeof(ApiResponse<DisAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminDetail(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("災害警示 ID 必須大於 0。");
        try
        {
            var item = await _context.DisasterAlerts.AsNoTracking().Include(x => x.AlertSegments)
                .FirstOrDefaultAsync(x => x.AlertId == id, cancellationToken);
            return item is null ? NotFoundResponse() : SuccessResponse(ToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢災害警示失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("新增災害警示")]
    [EndpointDescription("僅限管理員。路段使用 GeoJSON；更新時省略路段保留原資料，提供陣列則取代原路段，空陣列清除路段。")]
    [ProducesResponseType(typeof(ApiResponse<DisAlertDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(DisAlertDto payload, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid
            || string.IsNullOrWhiteSpace(payload.AlertType)
            || string.IsNullOrWhiteSpace(payload.AlertTitle)
            || !HasValidSegments(payload.AlertSegments)
            || payload.EffectiveFrom == default
            || (payload.EffectiveTo.HasValue && payload.EffectiveTo.Value < payload.EffectiveFrom))
        {
            return ErrorResponse("請檢查災害警示欄位、有效起訖時間與 GeoJSON。");
        }
        try
        {
            var item = new DisasterAlert()
            {
                AlertType = payload.AlertType.Trim(),
                AlertTitle = payload.AlertTitle.Trim(),
                AlertDescription = payload.AlertDescription?.Trim(),
                SeverityLevel = payload.SeverityLevel,
                EffectiveFrom = payload.EffectiveFrom,
                EffectiveTo = payload.EffectiveTo,
                SourceAgency = payload.SourceAgency?.Trim(),
                SourceUrl = payload.SourceUrl?.Trim(),
                IsActive = payload.IsActive,
            };
            if (payload.AlertSegments is not null)
            {
                foreach (var segment in payload.AlertSegments)
                {
                    item.AlertSegments.Add(new AlertSegment
                    {
                        SegmentName = segment.SegmentName,
                        Shape = segment.Shape,
                        SourceFeatureId = segment.SourceFeatureId,
                        SegmentLevel = segment.SegmentLevel,
                        Description = segment.Description,
                    });
                }
            }
            _context.DisasterAlerts.Add(item);
            await _context.SaveChangesAsync(cancellationToken);
            return CreatedResponse(ToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增災害警示失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("更新災害警示")]
    [EndpointDescription("僅限管理員。路段使用 GeoJSON；更新時省略路段保留原資料，提供陣列則取代原路段，空陣列清除路段。")]
    [ProducesResponseType(typeof(ApiResponse<DisAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(long id, DisAlertDto payload, CancellationToken cancellationToken)
    {
        if (id <= 0 || id != payload.AlertId) return ErrorResponse("路由 ID 與本文 ID 必須相同且大於 0。");
        if (!ModelState.IsValid
            || string.IsNullOrWhiteSpace(payload.AlertType)
            || string.IsNullOrWhiteSpace(payload.AlertTitle)
            || !HasValidSegments(payload.AlertSegments)
            || payload.EffectiveFrom == default
            || (payload.EffectiveTo.HasValue && payload.EffectiveTo.Value < payload.EffectiveFrom))
        {
            return ErrorResponse("請檢查災害警示欄位、有效起訖時間與 GeoJSON。");
        }
        try
        {
            var item = await _context.DisasterAlerts.Include(x => x.AlertSegments)
                .FirstOrDefaultAsync(x => x.AlertId == id, cancellationToken);
            if (item is null) return NotFoundResponse();
            item.AlertType = payload.AlertType.Trim();
            item.AlertTitle = payload.AlertTitle.Trim();
            item.AlertDescription = payload.AlertDescription?.Trim();
            item.SeverityLevel = payload.SeverityLevel;
            item.EffectiveFrom = payload.EffectiveFrom;
            item.EffectiveTo = payload.EffectiveTo;
            item.SourceAgency = payload.SourceAgency?.Trim();
            item.SourceUrl = payload.SourceUrl?.Trim();
            item.IsActive = payload.IsActive;
            if (payload.AlertSegments is not null)
            {
                _context.AlertSegments.RemoveRange(item.AlertSegments);
                item.AlertSegments.Clear();
                foreach (var segment in payload.AlertSegments)
                {
                    item.AlertSegments.Add(new AlertSegment
                    {
                        SegmentName = segment.SegmentName,
                        Shape = segment.Shape,
                        SourceFeatureId = segment.SourceFeatureId,
                        SegmentLevel = segment.SegmentLevel,
                        Description = segment.Description,
                    });
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
            return SuccessResponse(ToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新災害警示失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("刪除災害警示")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("災害警示 ID 必須大於 0。");
        try
        {
            var item = await _context.DisasterAlerts.Include(x => x.AlertSegments)
                .FirstOrDefaultAsync(x => x.AlertId == id, cancellationToken);
            if (item is null) return NotFoundResponse();
            if (await _context.AlertsTrails.AnyAsync(x => x.AlertId == id, cancellationToken))
            {
                return ErrorResponse("災害警示仍有步道關聯資料，無法刪除。", statusCode: StatusCodes.Status409Conflict);
            }
            _context.AlertSegments.RemoveRange(item.AlertSegments);
            _context.DisasterAlerts.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
            return SuccessResponse<string>("", message: "刪除資料成功！");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            _logger.LogError(ex, "刪除災害警示失敗 (Id: {Id})", id);
            return ErrorResponse("資料目前無法刪除，請確認關聯資料。", statusCode: StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除災害警示失敗 (Id: {Id})", id);
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
