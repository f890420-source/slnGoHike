using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers;
using Microsoft.AspNetCore.Authorization;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.GoHikeSafe;

[ApiController]
[Route("api/indicators")]
public class IndicatorApiController : BaseController
{
    private readonly GoHikeDataContext _context;
    private readonly ILogger<IndicatorApiController> _logger;

    public IndicatorApiController(
        GoHikeDataContext context,
        ILogger<IndicatorApiController> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<IndicatorTextInfoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        // only text, no geometry
        var listResultQuery = _context.Indicators
            .Where(x => x.IsActive == true)
            .Select(x => new IndicatorTextInfoDto()
            {
                id = x.IndicatorId,
                IndicatorName = x.IndicatorName,
                IndicatorType = x.IndicatorType
            });

        try
        {
            var listResult = await listResultQuery.ToListAsync(cancellationToken);
            _logger.LogInformation($"拿到 {listResult.Count()} 筆指標資料");
            return SuccessResponse(listResult);
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.GetType()}: {ex.Message}");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<IndicatorPublicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return ErrorResponse("指標 ID 必須大於 0。");
        }
        var idResultQuery = _context.Indicators
            .Where(x => x.IsActive == true && x.IndicatorId == id)
            .Select(x => new IndicatorPublicDto()
            {
                id = x.IndicatorId,
                IndicatorName = x.IndicatorName,
                IndicatorType = x.IndicatorType,
                IndiSegments = x.IndicatorSegments
                    .Select(s => new IndicatorSegmentPublicDto()
                    {
                        id = s.IndicatorSegmentId,
                        SegmentName = s.SegmentName,
                        Shape = s.Shape
                    }).ToList()
            });

        try
        {
            var idResult = await idResultQuery.FirstOrDefaultAsync(cancellationToken);
            if (idResult is not null)
            {
                _logger.LogInformation($"拿到 {idResult.IndicatorName} (編號: {id}) 的指標資料");
                return SuccessResponse(idResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.GetType()}: {ex.Message}");
            _logger.LogError(ex.StackTrace);
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
        return NotFoundResponse($"找不到編號為 {id} 的指標！");
    }

    private static IndicatorDto ToDto(Indicator item) => new()
    {
        id = item.IndicatorId,
        IndicatorName = item.IndicatorName,
        IndicatorType = item.IndicatorType,
        Weight = item.Weight,
        IndicatorLevel = item.IndicatorLevel,
        IndicatorDescription = item.IndicatorDescription,
        DataSource = item.DataSource,
        IsActive = item.IsActive,
        IndicatorSegments = item.IndicatorSegments.Select(s => new IndicatorSegmentDto
        {
            id = s.IndicatorSegmentId,
            SegmentName = s.SegmentName,
            Shape = s.Shape,
            SourceFeatureId = s.SourceFeatureId,
            SegmentLevel = s.SegmentLevel,
            Description = s.Description,
        }).ToList()
    };

    private static bool HasValidSegments(IEnumerable<IndicatorSegmentDto>? segments) =>
        segments is null || segments.All(segment =>
            segment is not null && segment.Shape is not null &&
            !segment.Shape.IsEmpty && segment.Shape.IsValid &&
            segment.Shape.Coordinates.All(point => double.IsFinite(point.X) &&
                double.IsFinite(point.Y) && Math.Abs(point.X) <= 180 && Math.Abs(point.Y) <= 90));

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("取得所有指標管理資料")]
    [ProducesResponseType(typeof(ApiResponse<List<IndicatorDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminList(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _context.Indicators.AsNoTracking().Include(x => x.IndicatorSegments)
                .OrderBy(x => x.IndicatorId).ToListAsync(cancellationToken);
            return SuccessResponse(items.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢指標失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("admin/{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("取得指定指標管理資料")]
    [ProducesResponseType(typeof(ApiResponse<IndicatorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminDetail(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("指標 ID 必須大於 0。");
        try
        {
            var item = await _context.Indicators.AsNoTracking().Include(x => x.IndicatorSegments)
                .FirstOrDefaultAsync(x => x.IndicatorId == id, cancellationToken);
            return item is null ? NotFoundResponse() : SuccessResponse(ToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢指標失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("新增指標")]
    [EndpointDescription("僅限管理員。路段使用 GeoJSON；更新時省略路段保留原資料，提供陣列則取代原路段，空陣列清除路段。")]
    [ProducesResponseType(typeof(ApiResponse<IndicatorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(IndicatorDto payload, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid
            || string.IsNullOrWhiteSpace(payload.IndicatorName)
            || string.IsNullOrWhiteSpace(payload.IndicatorType)
            || !HasValidSegments(payload.IndicatorSegments))
        {
            return ErrorResponse("請檢查指標欄位與 GeoJSON。");
        }
        try
        {
            var item = new Indicator()
            {
                IndicatorName = payload.IndicatorName.Trim(),
                IndicatorType = payload.IndicatorType.Trim(),
                Weight = payload.Weight,
                IndicatorLevel = payload.IndicatorLevel,
                IndicatorDescription = payload.IndicatorDescription?.Trim(),
                DataSource = payload.DataSource?.Trim(),
                IsActive = payload.IsActive,
            };
            if (payload.IndicatorSegments is not null)
            {
                foreach (var segment in payload.IndicatorSegments)
                {
                    item.IndicatorSegments.Add(new IndicatorSegment
                    {
                        SegmentName = segment.SegmentName,
                        Shape = segment.Shape,
                        SourceFeatureId = segment.SourceFeatureId,
                        SegmentLevel = segment.SegmentLevel,
                        Description = segment.Description,
                    });
                }
            }
            _context.Indicators.Add(item);
            await _context.SaveChangesAsync(cancellationToken);
            return CreatedResponse(ToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增指標失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("更新指標")]
    [EndpointDescription("僅限管理員。路段使用 GeoJSON；更新時省略路段保留原資料，提供陣列則取代原路段，空陣列清除路段。")]
    [ProducesResponseType(typeof(ApiResponse<IndicatorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(long id, IndicatorDto payload, CancellationToken cancellationToken)
    {
        if (id <= 0 || id != payload.id) return ErrorResponse("路由 ID 與本文 ID 必須相同且大於 0。");
        if (!ModelState.IsValid
            || string.IsNullOrWhiteSpace(payload.IndicatorName)
            || string.IsNullOrWhiteSpace(payload.IndicatorType)
            || !HasValidSegments(payload.IndicatorSegments))
        {
            return ErrorResponse("請檢查指標欄位與 GeoJSON。");
        }
        try
        {
            var item = await _context.Indicators.Include(x => x.IndicatorSegments)
                .FirstOrDefaultAsync(x => x.IndicatorId == id, cancellationToken);
            if (item is null) return NotFoundResponse();
            item.IndicatorName = payload.IndicatorName.Trim();
            item.IndicatorType = payload.IndicatorType.Trim();
            item.Weight = payload.Weight;
            item.IndicatorLevel = payload.IndicatorLevel;
            item.IndicatorDescription = payload.IndicatorDescription?.Trim();
            item.DataSource = payload.DataSource?.Trim();
            item.IsActive = payload.IsActive;
            if (payload.IndicatorSegments is not null)
            {
                _context.IndicatorSegments.RemoveRange(item.IndicatorSegments);
                item.IndicatorSegments.Clear();
                foreach (var segment in payload.IndicatorSegments)
                {
                    item.IndicatorSegments.Add(new IndicatorSegment
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
            _logger.LogError(ex, "更新指標失敗");
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("刪除指標")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return ErrorResponse("指標 ID 必須大於 0。");
        try
        {
            var item = await _context.Indicators.Include(x => x.IndicatorSegments)
                .FirstOrDefaultAsync(x => x.IndicatorId == id, cancellationToken);
            if (item is null) return NotFoundResponse();
            if (await _context.TrailIndicators.AnyAsync(x => x.IndicatorId == id, cancellationToken))
            {
                return ErrorResponse("指標仍有步道關聯資料，無法刪除。", statusCode: StatusCodes.Status409Conflict);
            }
            _context.IndicatorSegments.RemoveRange(item.IndicatorSegments);
            _context.Indicators.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
            return SuccessResponse<string>("", message: "刪除資料成功！");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            _logger.LogError(ex, "刪除指標失敗 (Id: {Id})", id);
            return ErrorResponse("資料目前無法刪除，請確認關聯資料。", statusCode: StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除指標失敗 (Id: {Id})", id);
            return ErrorResponse("發生錯誤，請洽管理員。", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
