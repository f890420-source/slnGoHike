using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers;
using prjGoHike.APIControllers.User;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.GoHikeSafe;

[ApiController]
[Route("api/indicators")]
//[Authorize]  // Disable Authorize when developing..
public class IndicatorApiController : BaseController
{
    private GoHikeDataContext _context;
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
    public async Task<IActionResult> List()
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
            var listResult = await listResultQuery.ToListAsync();
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
    public async Task<IActionResult> List(long id)
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
            var idResult = await idResultQuery.FirstOrDefaultAsync();
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
        return NotFoundResponse($"找不到編號為 {id} 的步道！");
    }
}
