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
        
        [HttpGet]
        [EndpointSummary("取得已發布的步道")]
        [EndpointDescription("回傳已發布步道及其路段資料，路段 geometry 使用 GeoJSON 格式。查無已發布步道時回傳成功與空陣列。")]
        [ProducesResponseType(typeof(ApiResponse<List<TrailPublicDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            if (!long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }   
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
                    _logger.LogInformation($"使用者 {userId} 索取步道清單一次");
                    return SuccessResponse(trailsResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.GetType()} (UserId: {userId}): {ex.Message}");
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
            if (!long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }
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
                    _logger.LogInformation($"使用者 {userId} 索取步道編號 {id} 一次");
                    return SuccessResponse(trailsResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.GetType()} (UserId: {userId}): {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }
            return NotFoundResponse("找不到步道!");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("新增步道")]
        [EndpointDescription("僅限管理員。以請求本文中的步道及路段資料新增步道；路段 Shape 使用 GeoJSON 格式，路段來源由伺服器設定。")]
        [ProducesResponseType(typeof(ApiResponse<Trail>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            TrailPublicDto payload,
            CancellationToken cancellationToken
            )
        {
            if (!long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }
            if (!ModelState.IsValid)
            {
                _logger.LogError("格式錯誤");
                return BadRequest();
            }
            var newTrail = new Trail()
            {
                TrailName = payload.TrailName.Trim(),
                Region = payload.Region.Trim(),
                DifficultyLevel = payload.DifficultyLevel,
                DistanceKm = payload.DistanceKm,
                TrailSegments = (payload.TrailSegDtos ?? [])
                    .Select(x => new TrailSegment
                    {
                        Source = "User Uploaded",
                        Shape = x.Shape
                    }).ToList()
            };
            _context.Trails.Add(newTrail);
            await _context.SaveChangesAsync();
            return CreatedResponse(newTrail);
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
        [EndpointSummary("更新步道")]
        [EndpointDescription("僅限管理員。以查詢參數 id 指定步道，且必須與請求本文中的 id 相同；更新步道欄位、取代路段資料，並將步道設為已發布。路段 Shape 使用 GeoJSON 格式。")]
        [ProducesResponseType(typeof(ApiResponse<TrailPublicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            long id,
            TrailPublicDto payload,
            CancellationToken cancellationToken
        )
        {
            if (!long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            {
                return Unauthorized();
            }
            if (id != payload.id || !ModelState.IsValid)
            {
                _logger.LogError("input 格式錯誤");
                return ErrorResponse("請檢查輸入格式",
 statusCode: StatusCodes.Status400BadRequest);
            }

            var trailquery = _context.Trails
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
                trail.IsPublished = true; //之後在 api 層控管是否公開
                trail.TrailSegments = (payload.TrailSegDtos ?? [])
                    .Select(x => new TrailSegment
                    {
                        Source = "User Uploaded",
                        Shape = x.Shape
                    }).ToList();
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.GetType()} (UserId: {userId}): {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return ErrorResponse("發生錯誤，請洽系統管理員。", statusCode: StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation($"編號 {id} 資料已經更新");
            return SuccessResponse(payload);
        }

        [HttpDelete]
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
