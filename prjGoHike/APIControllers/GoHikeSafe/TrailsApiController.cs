using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;
using NetTopologySuite.Geometries;
using Microsoft.AspNetCore.Authorization;
using prjGoHike.APIControllers.User;
using System.Security.Claims;

namespace prjGoHike.APIControllers.GoHikeSafe

{
    [ApiController]
    [Route("api/trails")]
    public class TrailsApiController : BaseController
    {
        private GoHikeDataContext _context;
        private readonly ILogger<LoginController> _logger;
        
        public TrailsApiController (
            GoHikeDataContext context,
            ILogger<LoginController> logger
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
                segment is not null && segment.Shape is LineString or MultiLineString &&
                !segment.Shape.IsEmpty && segment.Shape.IsValid &&
                segment.Shape.Coordinates.All(point => double.IsFinite(point.X) &&
                    double.IsFinite(point.Y) && Math.Abs(point.X) <= 180 && Math.Abs(point.Y) <= 90));

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("取得所有步道管理資料")]
        [ProducesResponseType(typeof(ApiResponse<List<TrailAdminDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AdminList(CancellationToken cancellationToken)
        {
            var trails = await _context.Trails.AsNoTracking()
                .Include(trail => trail.TrailSegments)
                .OrderBy(trail => trail.TrailId)
                .ToListAsync(cancellationToken);
            return SuccessResponse(trails.Select(ToAdminDto).ToList());
        }

        [HttpGet("admin/{id:long}")]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("取得指定步道管理資料")]
        [ProducesResponseType(typeof(ApiResponse<TrailAdminDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AdminDetail(long id, CancellationToken cancellationToken)
        {
            var trail = await _context.Trails.AsNoTracking()
                .Include(item => item.TrailSegments)
                .FirstOrDefaultAsync(item => item.TrailId == id, cancellationToken);
            return trail is null ? NotFoundResponse() : SuccessResponse(ToAdminDto(trail));
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
            _context.Trails.Add(newTrail);
            await _context.SaveChangesAsync();
            return CreatedResponse(ToAdminDto(newTrail));
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("更新步道")]
        [EndpointDescription("僅限管理員。查詢參數 id 須與本文 id 相同；省略路段時保留原路線，提供路段時以有效 GeoJSON 路線取代。")]
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
            if (id != payload.id || !ModelState.IsValid || string.IsNullOrWhiteSpace(payload.TrailName)
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
        [EndpointDescription("僅限管理員。以查詢參數 id 指定步道，刪除該步道及其路段；成功時回傳訊息與空字串資料。")]
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
            var trailIdDb = await _context.Trails
                .Include(t => t.TrailSegments)
                .FirstOrDefaultAsync(m => m.TrailId == id);
            if(trailIdDb is null)
            {
                return NotFoundResponse();
            }
            _context.TrailSegments.RemoveRange(trailIdDb.TrailSegments);
            _context.Trails.Remove(trailIdDb);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"編號 {id} 資料已遭刪除");
                return SuccessResponse<string>("", message: "刪除資料成功！");
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
