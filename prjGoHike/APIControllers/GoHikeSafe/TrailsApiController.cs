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
    [Authorize]
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
        public async Task<IActionResult> List()
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
                    .ToListAsync();
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
                return ErrorResponse("發生錯誤，請洽管理員。");
            }
            return NotFoundResponse("找不到步道!");
        }

        [HttpGet("{id:long}")]
        [Authorize]
        public async Task<IActionResult> List(long id)
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
                    .FirstOrDefaultAsync();
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
                return ErrorResponse("發生錯誤，請洽管理員。");
            }
            return NotFoundResponse("找不到步道!");
        }
    }
}
