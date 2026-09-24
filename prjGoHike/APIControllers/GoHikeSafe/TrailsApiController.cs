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
                DistanceKm = payload.DistanceKm
            };
            return Ok(payload);
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
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
                await _context.SaveChangesAsync();
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
        public async Task<IActionResult> Delete(long id)
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
                await _context.SaveChangesAsync();
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
