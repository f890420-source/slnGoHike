using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers;
using prjGoHike.APIControllers.User;
using prjGoHike.Models;

namespace prjGoHike.DTO.GoHikeSafe;

[ApiController]
[Route("api/indicators")]
public class IndicatorApiController : BaseController
{
    private GoHikeDataContext _context;
    private readonly ILogger<LoginController> _logger;

    public IndicatorApiController(
        GoHikeDataContext context,
        ILogger<LoginController> logger
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
            if (listResult is not null)
            {
                _logger.LogInformation($"拿到 {listResult.Count()} 筆指標資料");
                return SuccessResponse(listResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.GetType()}: {ex.Message}");
            return ErrorResponse("發生錯誤，請洽管理員。");
        }
        return NotFoundResponse("找不到指標!");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> List(int id)
    {
        var idResultQuery = _context.Indicators
            .Where(x => x.IsActive == true && x.IndicatorId == id)
            .Select(x => new IndicatorPublicDto()
            {
                id = x.IndicatorId,
                IndicatorName = x.IndicatorName,
                IndicatorType = x.IndicatorType,
                IndiSegDtos = x.IndicatorSegments
                    .Select(s => new IndicatorSegmentPublicDto()
                    {
                        id = s.IndicatorSegmentId,
                        SegmentName = s.SegmentName,
                        Shape = s.Shape
                    })
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
            return ErrorResponse("發生錯誤，請洽管理員。");
        }
        return NotFoundResponse($"找不到編號為 {id} 的步道！");
    }
}
