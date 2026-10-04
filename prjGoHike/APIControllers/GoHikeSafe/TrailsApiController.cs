using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;
using prjGoHike.Services;
using NetTopologySuite.Geometries;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace prjGoHike.APIControllers.GoHikeSafe

{
    [ApiController]
    [Route("api/trails")]
    public class TrailsApiController : BaseController
    {
        private readonly GoHikeDataContext _context;
        private readonly ILogger<TrailsApiController> _logger;
        
        public TrailsApiController (
            GoHikeDataContext context,
            ILogger<TrailsApiController> logger
        )
        {
            _context = context;
            _logger = logger;
        }

        private static TrailAdminDto ToAdminDto(Trail trail) => new()
        {
            id = trail.TrailId,
            TrailName = trail.TrailName,
            Region = trail.Region,
            DifficultyLevel = trail.DifficultyLevel,
            DistanceKm = trail.DistanceKm,
            EstimatedHours = trail.EstimatedHours,
            PermitRequired = trail.PermitRequired,
            GuideRequired = trail.GuideRequired,
            RegulationNote = trail.RegulationNote,
            IsPublished = trail.IsPublished,
            TrailSegDtos = trail.TrailSegments.Select(segment => new TrailAdminSegmentDto
            {
                id = segment.TrailSegmentId,
                Shape = segment.Shape
            }).ToList()
        };

        private static bool HasValidSegments(IEnumerable<TrailAdminSegmentDto>? segments) =>
            segments is not null && segments.Any() && segments.All(segment =>
                segment is not null && segment.Shape is LineString or MultiLineString);

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("取得所有步道管理資料")]
        [ProducesResponseType(typeof(ApiResponse<List<TrailAdminDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AdminList(CancellationToken cancellationToken)
        {
            try
            {
                var trails = await _context.Trails.AsNoTracking()
                    .Include(trail => trail.TrailSegments)
                    .OrderBy(trail => trail.TrailId)
                    .ToListAsync(cancellationToken);
                return SuccessResponse(trails.Select(ToAdminDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢步道管理資料失敗");
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("admin/{id:long}")]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("取得指定步道管理資料")]
        [ProducesResponseType(typeof(ApiResponse<TrailAdminDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AdminDetail(long id, CancellationToken cancellationToken)
        {
            if (id <= 0) return ErrorResponse("步道 ID 必須大於 0。");
            try
            {
                var trail = await _context.Trails.AsNoTracking()
                    .Include(item => item.TrailSegments)
                    .FirstOrDefaultAsync(item => item.TrailId == id, cancellationToken);
                return trail is null ? NotFoundResponse() : SuccessResponse(ToAdminDto(trail));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢步道管理資料失敗 (Id: {Id})", id);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        
        [HttpGet]
        [EndpointSummary("取得已發布的步道")]
        [EndpointDescription("回傳已發布步道及其路段資料，路段 geometry 使用 GeoJSON 格式。查無已發布步道時回傳成功與空陣列。")]
        [ProducesResponseType(typeof(ApiResponse<List<TrailPublicDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            var trailsQuery = _context.Trails.Where(x => x.IsPublished == true).Select(
                x => new TrailPublicDto()
                {
                    id = x.TrailId,
                    TrailName = x.TrailName,
                    Region = x.Region,
                    DifficultyLevel = x.DifficultyLevel,
                    DistanceKm = x.DistanceKm,
                    TrailSegDtos = x.TrailSegments.Select(s => new TrailSegmentPublicDto()
                            {
                                id = s.TrailSegmentId,
                                Source = s.Source,
                                Shape = s.Shape
                            })
                }
            );

            try
            {
                var trailsResult = await trailsQuery
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                if (trailsResult is not null)
                {
                    return SuccessResponse(trailsResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.GetType()}: {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }
            return NotFoundResponse("找不到步道!");
        }

        [HttpGet("{id:long}")]
        [EndpointSummary("取得指定的已發布步道")]
        [EndpointDescription("依路由中的步道 ID 取得已發布步道及其路段資料，路段 Shape 使用 GeoJSON 格式；找不到時回傳 404。")]
        [ProducesResponseType(typeof(ApiResponse<TrailPublicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> List(long id, CancellationToken cancellationToken)
        {
            if (id <= 0) return ErrorResponse("步道 ID 必須大於 0。");
            var trailsQuery = _context.Trails.Where(x => 
                x.IsPublished == true 
                && x.TrailId == id
                ).Select(
                x => new TrailPublicDto()
                {
                    id = x.TrailId,
                    TrailName = x.TrailName,
                    Region = x.Region,
                    DifficultyLevel = x.DifficultyLevel,
                    DistanceKm = x.DistanceKm,
                    TrailSegDtos = x.TrailSegments.Select(s => new TrailSegmentPublicDto()
                            {
                                id = s.TrailSegmentId,
                                Source = s.Source,
                                Shape = s.Shape
                            })
                }
            );

            try
            {
                var trailsResult = await trailsQuery
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);
                if (trailsResult is not null)
                {
                    return SuccessResponse(trailsResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.GetType()}: {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }
            return NotFoundResponse("找不到步道!");
        }

        [HttpGet("{id:long}/indicators")]
        [EndpointSummary("取得指定已發布步道的關聯指標")]
        [EndpointDescription("從 TrailIndicators 回傳啟用指標的精簡清單，包含未評分與已評分關聯。沒有關聯時回傳 200、hasIndicators=false 與空清單；步道不存在或未發布時回傳 404。此查詢不觸發 Spatial Join。")]
        [ProducesResponseType(typeof(ApiResponse<TrailIndicatorsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Indicators(long id, CancellationToken cancellationToken)
        {
            if (id <= 0) return ErrorResponse("步道 ID 必須大於 0。");
            try
            {
                var result = await TrailIndicatorQuery.ForPublishedTrail(_context, id)
                    .FirstOrDefaultAsync(cancellationToken);
                return result is null ? NotFoundResponse("找不到步道!") : SuccessResponse(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢步道關聯指標失敗 (Id: {Id})", id);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("新增步道")]
        [EndpointDescription("僅限管理員。以 JSON 新增所有步道欄位及至少一段有效 GeoJSON 路線。")]
        [ProducesResponseType(typeof(ApiResponse<TrailAdminDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            TrailAdminDto payload,
            CancellationToken cancellationToken
            )
        {
            if (!long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(payload.TrailName)
                || string.IsNullOrWhiteSpace(payload.Region) || !HasValidSegments(payload.TrailSegDtos))
            {
                return ErrorResponse("請檢查步道欄位與 GeoJSON 路線。");
            }
            var newTrail = new Trail()
            {
                TrailName = payload.TrailName.Trim(),
                Region = payload.Region.Trim(),
                DifficultyLevel = payload.DifficultyLevel,
                DistanceKm = payload.DistanceKm,
                EstimatedHours = payload.EstimatedHours,
                PermitRequired = payload.PermitRequired,
                GuideRequired = payload.GuideRequired,
                RegulationNote = payload.RegulationNote?.Trim(),
                IsPublished = payload.IsPublished,
                TrailSegments = (payload.TrailSegDtos ?? [])
                    .Select(x => new TrailSegment
                    {
                        Source = "User Uploaded",
                        Shape = x.Shape
                    }).ToList()
            };
            try
            {
                _context.Trails.Add(newTrail);
                await _context.SaveChangesAsync(cancellationToken);
                return CreatedResponse(ToAdminDto(newTrail));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增步道失敗 (UserId: {UserId})", userId);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("更新步道")]
        [EndpointDescription("僅限管理員。路由參數 id 須與本文 id 相同；省略路段時保留原路線，提供路段時以有效 GeoJSON 路線取代。")]
        [ProducesResponseType(typeof(ApiResponse<TrailAdminDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            long id,
            TrailAdminDto payload,
            CancellationToken cancellationToken
        )
        {
            if (!long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }
            if (id <= 0 || id != payload.id || !ModelState.IsValid || string.IsNullOrWhiteSpace(payload.TrailName)
                || string.IsNullOrWhiteSpace(payload.Region)
                || (payload.TrailSegDtos is not null && !HasValidSegments(payload.TrailSegDtos)))
            {
                _logger.LogError("input 格式錯誤");
                return ErrorResponse("請檢查輸入格式",
 statusCode: StatusCodes.Status400BadRequest);
            }

            var trailquery = _context.Trails
                .Include(t => t.TrailSegments)
                .Where(t => t.TrailId == id);
            
            try
            {
                var trail = await trailquery.FirstOrDefaultAsync(cancellationToken);

                if (trail is null)
                {
                    return NotFoundResponse();
                }

                // 更新 Trail 的一般欄位。
                trail.TrailName = payload.TrailName.Trim();
                trail.Region = payload.Region.Trim();
                trail.DifficultyLevel = payload.DifficultyLevel;
                trail.DistanceKm = payload.DistanceKm;
                trail.EstimatedHours = payload.EstimatedHours;
                trail.PermitRequired = payload.PermitRequired;
                trail.GuideRequired = payload.GuideRequired;
                trail.RegulationNote = payload.RegulationNote?.Trim();
                trail.IsPublished = payload.IsPublished;
                if (payload.TrailSegDtos is not null)
                {
                    _context.TrailSegments.RemoveRange(trail.TrailSegments);
                    trail.TrailSegments.Clear();
                    foreach (var segment in payload.TrailSegDtos)
                    {
                        trail.TrailSegments.Add(new TrailSegment
                        {
                            Source = "User Uploaded",
                            Shape = segment.Shape
                        });
                    }
                }
                await _context.SaveChangesAsync(cancellationToken);
                return SuccessResponse(ToAdminDto(trail));
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.GetType()} (UserId: {userId}): {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }

        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("刪除步道")]
        [EndpointDescription("僅限管理員。以路由參數 id 指定步道，刪除該步道及其路段；成功時回傳訊息與空字串資料。")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(
            long id,
            CancellationToken cancellationToken
        )
        {
            if (!long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }
            if (id <= 0) return ErrorResponse("步道 ID 必須大於 0。");
            try
            {
                var trailIdDb = await _context.Trails
                    .Include(t => t.TrailSegments)
                    .FirstOrDefaultAsync(t => t.TrailId == id, cancellationToken);
                if (trailIdDb is null) return NotFoundResponse();
                if (await _context.AlertsTrails.AnyAsync(x => x.TrailId == id, cancellationToken)
                    || await _context.HikeRecordDetails.AnyAsync(x => x.TrailId == id, cancellationToken)
                    || await _context.TrailFeatures.AnyAsync(x => x.TrailId == id, cancellationToken)
                    || await _context.TrailIndicators.AnyAsync(x => x.TrailId == id, cancellationToken)
                    || await _context.TrailSubscriptions.AnyAsync(x => x.TrailId == id, cancellationToken)
                    || await _context.TripReports.AnyAsync(x => x.TrailId == id, cancellationToken))
                {
                    return ErrorResponse("步道仍有關聯資料，無法刪除。", statusCode: StatusCodes.Status409Conflict);
                }
                _context.TrailSegments.RemoveRange(trailIdDb.TrailSegments);
                _context.Trails.Remove(trailIdDb);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"編號 {id} 資料已遭刪除");
                return SuccessResponse<string>("", message: "刪除資料成功！");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            {
                _logger.LogError(ex, "刪除步道失敗 (Id: {Id})", id);
                return ErrorResponse("資料目前無法刪除，請確認關聯資料。", statusCode: StatusCodes.Status409Conflict);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.GetType()} (UserId: {userId}): {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
