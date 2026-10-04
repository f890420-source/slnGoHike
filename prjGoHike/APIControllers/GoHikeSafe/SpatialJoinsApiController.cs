using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using prjGoHike.DTO.GoHikeSafe;
using prjGoHike.Models;
using prjGoHike.Services.SpatialJoins;

namespace prjGoHike.APIControllers.GoHikeSafe;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/spatial-joins")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
public sealed class SpatialJoinsApiController(ISpatialJoinService service, ILogger<SpatialJoinsApiController> logger) : BaseController
{
    /// <summary>建立空間關聯同步工作；接受請求不代表同步完成。</summary>
    [HttpPost]
    [EndpointSummary("建立空間關聯同步工作")]
    [EndpointDescription("回傳 202 與狀態查詢網址；接受請求不代表同步完成。僅同步未評分關聯。")]
    [ProducesResponseType(typeof(ApiResponse<SpatialJoinRunDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<SpatialJoinRunDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(SpatialJoinRequestDto request, CancellationToken cancellationToken) =>
        RespondAsync(async () =>
        {
            var result = await service.CreateAsync(request.DistanceMeters!.Value, cancellationToken);
            var response = new ApiResponse<SpatialJoinRunDto>
            {
                Success = result.Created,
                Message = result.Created ? "已接受空間關聯同步請求。" : "已有空間關聯工作執行中，請先查詢其狀態。",
                Data = result.Run
            };
            if (!result.Created) return Conflict(response);
            return AcceptedAtAction(nameof(Get), new { id = result.Run.Id }, response);
        }, cancellationToken);

    /// <summary>查詢同步工作的狀態、UTC 時間與受控錯誤摘要。</summary>
    [HttpGet("{id:long}")]
    [EndpointSummary("查詢空間關聯工作狀態")]
    [ProducesResponseType(typeof(ApiResponse<SpatialJoinRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get(long id, CancellationToken cancellationToken) =>
        RespondAsync(async () =>
        {
            if (id <= 0) return ErrorResponse("工作 ID 必須大於 0。");
            var run = await service.GetAsync(id, cancellationToken);
            return run is null ? NotFoundResponse("找不到指定的空間關聯工作。") : SuccessResponse(run);
        }, cancellationToken);

    /// <summary>由新到舊查詢同步工作紀錄。</summary>
    [HttpGet]
    [EndpointSummary("查詢空間關聯工作紀錄")]
    [ProducesResponseType(typeof(ApiResponse<SpatialJoinPageDto<SpatialJoinRunDto>>), StatusCodes.Status200OK)]
    public Task<IActionResult> List([FromQuery] SpatialJoinQueryDto query, CancellationToken cancellationToken) =>
        RespondAsync(async () => SuccessResponse(await service.ListAsync(query, cancellationToken)), cancellationToken);

    /// <summary>查詢目前未評分關聯；不是指定 Run 的歷史結果，後續同步可能更改內容。</summary>
    [HttpGet("associations")]
    [EndpointSummary("查詢目前未評分候選關聯")]
    [EndpointDescription("回傳目前資料表的候選關聯，不是指定 Run 的歷史結果；後續同步可能更改內容。")]
    [ProducesResponseType(typeof(ApiResponse<SpatialJoinPageDto<SpatialJoinAssociationDto>>), StatusCodes.Status200OK)]
    public Task<IActionResult> Associations([FromQuery] SpatialJoinAssociationQueryDto query, CancellationToken cancellationToken) =>
        RespondAsync(async () => SuccessResponse(await service.GetAssociationsAsync(query, cancellationToken)), cancellationToken);

    private async Task<IActionResult> RespondAsync(Func<Task<IActionResult>> action, CancellationToken cancellationToken)
    {
        try { return await action(); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (SpatialJoinUnavailableException) { return ErrorResponse("空間關聯背景工作尚未啟用。", statusCode: StatusCodes.Status503ServiceUnavailable); }
        catch (Exception ex) when (ex is DbException or TimeoutException or SpatialJoinLockException)
        {
            logger.LogError(ex, "SpatialJoin API storage operation failed");
            return ErrorResponse("暫時無法存取空間關聯工作，請稍後重試。", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
