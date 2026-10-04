using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers;
using prjGoHike.DTO.PersonalEquipment;
using prjGoHike.Models;
using prjGoHike.Services.PersonalEquipment;
using System.Security.Claims;

namespace prjGoHike.APIControllers.PersonalEquipment;

/// <summary>
/// 個人裝備清單的明細操作 API。
/// 負責新增自訂裝備、修改數量、更新準備狀態及移除單件裝備。
/// 不負責建立清單、查詢清單或修改清單名稱。
/// </summary>
/// <remarks>
/// 保留既有 API 路徑，前端不需要修改網址。
/// 每個操作都必須檢查登入會員是否擁有目標清單。
/// </remarks>
[Route("api/v1/PersonalEquipmentApi")]
public class PersonalEquipmentItemsController : BaseController
{
    private readonly GoHikeDataContext _db;
    private readonly EquipmentWeightService _weightService;

    /// <summary>
    /// 注入資料庫與配重服務。
    /// 裝備異動後的配重計算統一交給 EquipmentWeightService。
    /// </summary>
    public PersonalEquipmentItemsController(
        GoHikeDataContext db,
        EquipmentWeightService weightService)
    {
        _db = db;
        _weightService = weightService;
    }

    /// <summary>
    /// 從登入資訊取得會員 ID。
    /// 不接受前端傳入的會員編號作為清單擁有權依據。
    /// </summary>
    private long? GetCurrentUserId()
    {
        string? userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (long.TryParse(userIdClaim, out long userId))
        {
            return userId;
        }

        return null;
    }

    #region 更新裝備準備狀態
    /// <summary>
    /// 設定自己清單內某件裝備的準備狀態，並更新清單修改時間。
    /// true 表示已準備，false 表示未準備；不變更數量與重量。
    /// </summary>
    /// <remarks>
    /// PATCH /api/v1/PersonalEquipmentApi/lists/{listId}/items/{detailId}/prepared
    /// 必須同時符合裝備 ID、清單 ID、會員身分及未刪除條件。
    /// 儲存時若偵測到並行修改衝突，回傳 409，請前端重新查詢。
    /// </remarks>
    [Authorize]
    [HttpPatch("lists/{listId:long}/items/{detailId:long}/prepared")]
    public async Task<IActionResult> UpdateEquipmentPrepared(
    long listId,
    long detailId,
    [FromBody] UpdateEquipmentPreparedRequestDto request,
    CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再更新裝備狀態",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!request.IsPrepared.HasValue)
        {
            return ErrorResponse("請提供裝備準備狀態");
        }

        var detail = await _db.PersonalEquipmentDetails
            .Include(item => item.List)
            .SingleOrDefaultAsync(
                item =>
                    item.DetailId == detailId &&
                    item.ListId == listId &&
                    item.List.MemberId == currentUserId.Value &&
                    !item.List.IsDeleted,
                cancellationToken);

        if (detail is null)
        {
            return NotFoundResponse("找不到指定的裝備明細");
        }

        detail.IsPrepared = request.IsPrepared.Value;
        detail.List.UpdatedAt = DateTime.Now;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ErrorResponse(
                "清單已被其他操作更新，請重新查詢後再試",
                statusCode: StatusCodes.Status409Conflict);
        }

        return SuccessResponse(
            new
            {
                detail.ListId,
                detail.DetailId,
                detail.IsPrepared
            },
            "裝備準備狀態更新成功");
    }
    #endregion

    #region 修改裝備數量
    /// <summary>
    /// 修改自己清單內某件裝備的數量，並重新計算整張清單的配重。
    /// 使用明細原本儲存的單件重量，不套用最新裝備基本資料。
    /// </summary>
    /// <remarks>
    /// PATCH /api/v1/PersonalEquipmentApi/lists/{listId}/items/{detailId}/quantity
    /// 明細變更與清單配重結果一起儲存。
    /// 儲存時偵測到並行修改衝突，回傳 409。
    /// </remarks>
    [Authorize]
    [HttpPatch("lists/{listId:long}/items/{detailId:long}/quantity")]
    public async Task<IActionResult> UpdateEquipmentQuantity(
        long listId,
        long detailId,
        [FromBody] UpdateEquipmentQuantityRequestDto request,
        CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再修改裝備數量",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!request.Quantity.HasValue ||
            request.Quantity.Value < 1 ||
            request.Quantity.Value > 20)
        {
            return ErrorResponse("裝備數量必須介於 1 至 20");
        }

        // 只讀取目前會員自己的有效清單，並載入所有明細。
        // 需要更新資料，因此不使用 AsNoTracking。
        var list = await _db.PersonalEquipmentLists
            .Include(value => value.PersonalEquipmentDetails)
            .SingleOrDefaultAsync(
                value =>
                    value.ListId == listId &&
                    value.MemberId == currentUserId.Value &&
                    !value.IsDeleted,
                cancellationToken);

        if (list is null)
        {
            return NotFoundResponse("找不到指定的裝備清單");
        }

        var detail = list.PersonalEquipmentDetails
            .SingleOrDefault(item => item.DetailId == detailId);

        if (detail is null)
        {
            return NotFoundResponse("找不到指定的裝備明細");
        }

        // 防止舊資料不符合計算服務的體重範圍。
        if (list.BodyWeightKg < 20M || list.BodyWeightKg > 300M)
        {
            return ErrorResponse("清單記錄的體重不符合計算範圍");
        }

        int newQuantity = request.Quantity.Value;

        // 先以 long 計算，避免整數乘法溢位。
        // 驗證完成前不修改明細。
        long totalWeightGramValue = 0;

        foreach (var item in list.PersonalEquipmentDetails)
        {
            int quantity = item.DetailId == detailId
                ? newQuantity
                : item.Quantity;

            if (item.UnitWeightGram < 0 || quantity < 1)
            {
                return ErrorResponse("清單內有不合理的裝備重量或數量");
            }

            long itemWeightGram =
                (long)item.UnitWeightGram * quantity;

            if (itemWeightGram > int.MaxValue)
            {
                return ErrorResponse("單項裝備重量超出允許範圍");
            }

            totalWeightGramValue += itemWeightGram;

            if (totalWeightGramValue > int.MaxValue)
            {
                return ErrorResponse("裝備總重量超出允許範圍");
            }
        }

        // 使用清單儲存的體重，重新計算配重摘要。
        var weightResult = _weightService.Calculate(
            (int)totalWeightGramValue,
            list.BodyWeightKg);

        detail.Quantity = newQuantity;
        detail.TotalWeightGram =
            (int)((long)detail.UnitWeightGram * newQuantity);

        list.TotalWeightGram = weightResult.TotalWeightGram;
        list.MaxCarryWeightGram = weightResult.MaxCarryWeightGram;
        list.RemainingWeightGram = weightResult.RemainingWeightGram;
        list.WeightPercentage = weightResult.BodyWeightPercentage;
        list.WeightStatus = weightResult.WeightStatus;
        list.UpdatedAt = DateTime.Now;

        // 單次 SaveChanges 將明細與清單更新一併儲存。
        // 清單的 RowVersion 用於偵測讀取後發生的並行修改。
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ErrorResponse(
                "清單已被其他操作更新，請重新查詢後再試",
                statusCode: StatusCodes.Status409Conflict);
        }

        return SuccessResponse(
            new
            {
                list.ListId,
                detail.DetailId,
                detail.Quantity,
                detail.TotalWeightGram,
                ListTotalWeightGram = list.TotalWeightGram,
                list.MaxCarryWeightGram,
                list.RemainingWeightGram,
                list.WeightPercentage,
                list.WeightStatus
            },
            "裝備數量更新成功");
    }
    #endregion

    #region 移除單件裝備
    /// <summary>
    /// 移除自己清單內的一筆裝備明細，並重新計算整張清單的配重。
    /// 不刪除全站共用裝備資料；移除最後一件時仍保留清單。
    /// </summary>
    /// <remarks>
    /// DELETE /api/v1/PersonalEquipmentApi/lists/{listId}/items/{detailId}
    /// 刪除明細與更新配重以同一次 SaveChanges 儲存。
    /// </remarks>
    [Authorize]
    [HttpDelete("lists/{listId:long}/items/{detailId:long}")]
    public async Task<IActionResult> DeleteEquipmentItem(
        long listId,
        long detailId,
        CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再移除裝備",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // 只載入目前會員自己的有效清單及其明細。
        var list = await _db.PersonalEquipmentLists
            .Include(value => value.PersonalEquipmentDetails)
            .SingleOrDefaultAsync(
                value =>
                    value.ListId == listId &&
                    value.MemberId == currentUserId.Value &&
                    !value.IsDeleted,
                cancellationToken);

        if (list is null)
        {
            return NotFoundResponse("找不到指定的裝備清單");
        }

        // 從這張清單找明細，避免操作到其他清單的裝備。
        var detail = list.PersonalEquipmentDetails
            .SingleOrDefault(item => item.DetailId == detailId);

        if (detail is null)
        {
            return NotFoundResponse("找不到指定的裝備明細");
        }

        if (list.BodyWeightKg < 20M || list.BodyWeightKg > 300M)
        {
            return ErrorResponse("清單記錄的體重不符合計算範圍");
        }

        // 排除即將移除的裝備，重新加總剩餘裝備。
        // 使用 long 進行乘法，避免 int 溢位。
        long totalWeightGramValue = 0;

        foreach (var item in list.PersonalEquipmentDetails)
        {
            if (item.DetailId == detailId)
            {
                continue;
            }

            if (item.UnitWeightGram < 0 || item.Quantity < 1)
            {
                return ErrorResponse("清單內有不合理的裝備重量或數量");
            }

            totalWeightGramValue +=
                (long)item.UnitWeightGram * item.Quantity;

            if (totalWeightGramValue > int.MaxValue)
            {
                return ErrorResponse("裝備總重量超出允許範圍");
            }
        }

        // 沒有剩餘裝備時，總重量為 0。
        var weightResult = _weightService.Calculate(
            (int)totalWeightGramValue,
            list.BodyWeightKg);

        // 只刪除這筆清單明細，不刪除它參照的共用裝備。
        _db.Set<PersonalEquipmentDetail>().Remove(detail);

        list.TotalWeightGram = weightResult.TotalWeightGram;
        list.MaxCarryWeightGram = weightResult.MaxCarryWeightGram;
        list.RemainingWeightGram = weightResult.RemainingWeightGram;
        list.WeightPercentage = weightResult.BodyWeightPercentage;
        list.WeightStatus = weightResult.WeightStatus;
        list.UpdatedAt = DateTime.Now;

        // 明細刪除與清單摘要更新一起儲存。
        // 若讀取後有人修改同一張清單，回傳衝突提示。
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ErrorResponse(
                "清單已被其他操作更新，請重新查詢後再試",
                statusCode: StatusCodes.Status409Conflict);
        }

        return SuccessResponse(
            new
            {
                list.ListId,
                DetailId = detailId,
                ListTotalWeightGram = list.TotalWeightGram,
                list.MaxCarryWeightGram,
                list.RemainingWeightGram,
                list.WeightPercentage,
                list.WeightStatus
            },
            "裝備移除成功");
    }
    #endregion

    #region 新增自訂裝備
    /// <summary>
    /// 在自己的既有清單中新增自訂裝備，並重新計算配重。
    /// 不新增全站共用裝備，自訂項目的 EquipmentId 保持為 null。
    /// </summary>
    /// <remarks>
    /// POST /api/v1/PersonalEquipmentApi/lists/{listId}/items/custom
    /// 新明細與清單配重摘要以同一次 SaveChanges 儲存。
    /// </remarks>
    [Authorize]
    [HttpPost("lists/{listId:long}/items/custom")]
    public async Task<IActionResult> AddCustomEquipment(
        long listId,
        [FromBody] CreateCustomEquipmentRequestDto request,
        CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再新增自訂裝備",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        string name = request.CustomEquipmentName?.Trim() ?? "";
        string? notes = request.Notes?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return ErrorResponse("裝備名稱必須為 1 至 100 個字元");
        }

        if (!request.UnitWeightGram.HasValue ||
            request.UnitWeightGram.Value < 1 ||
            request.UnitWeightGram.Value > 100000)
        {
            return ErrorResponse("單件重量必須介於 1 至 100000 公克");
        }

        if (!request.Quantity.HasValue ||
            request.Quantity.Value < 1 ||
            request.Quantity.Value > 20)
        {
            return ErrorResponse("裝備數量必須介於 1 至 20");
        }

        if (notes is not null && notes.Length > 300)
        {
            return ErrorResponse("備註最多 300 個字元");
        }

        // 只允許新增到目前會員自己的有效清單。
        var list = await _db.PersonalEquipmentLists
            .Include(value => value.PersonalEquipmentDetails)
            .SingleOrDefaultAsync(
                value =>
                    value.ListId == listId &&
                    value.MemberId == currentUserId.Value &&
                    !value.IsDeleted,
                cancellationToken);

        if (list is null)
        {
            return NotFoundResponse("找不到指定的裝備清單");
        }

        if (list.BodyWeightKg < 20M || list.BodyWeightKg > 300M)
        {
            return ErrorResponse("清單記錄的體重不符合計算範圍");
        }

        int unitWeightGram = request.UnitWeightGram.Value;
        int quantity = request.Quantity.Value;
        long customTotalWeightGram = (long)unitWeightGram * quantity;

        // 用儲存的單件重量與數量重新加總。
        // 先用 long 計算，避免 int 乘法溢位。
        long totalWeightGramValue = customTotalWeightGram;

        foreach (var item in list.PersonalEquipmentDetails)
        {
            if (item.UnitWeightGram < 0 || item.Quantity < 1)
            {
                return ErrorResponse("清單內有不合理的裝備重量或數量");
            }

            long itemWeightGram =
                (long)item.UnitWeightGram * item.Quantity;

            totalWeightGramValue += itemWeightGram;

            if (totalWeightGramValue > int.MaxValue)
            {
                return ErrorResponse("裝備總重量超出允許範圍");
            }
        }

        // 新裝備放在目前明細的最後面。
        int maxSortOrder = list.PersonalEquipmentDetails
            .Select(item => item.SortOrder)
            .DefaultIfEmpty(0)
            .Max();

        if (maxSortOrder == int.MaxValue)
        {
            return ErrorResponse("裝備排序已達上限");
        }

        var weightResult = _weightService.Calculate(
            (int)totalWeightGramValue,
            list.BodyWeightKg);

        var detail = new PersonalEquipmentDetail
        {
            EquipmentId = null,
            CustomEquipmentName = name,
            Quantity = quantity,
            UnitWeightGram = unitWeightGram,
            TotalWeightGram = (int)customTotalWeightGram,

            // 自訂項目不自動判定為必備或建議。
            RequirementLevel = null,
            IsPrepared = false,
            SortOrder = maxSortOrder + 1,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes
        };

        list.PersonalEquipmentDetails.Add(detail);

        list.TotalWeightGram = weightResult.TotalWeightGram;
        list.MaxCarryWeightGram = weightResult.MaxCarryWeightGram;
        list.RemainingWeightGram = weightResult.RemainingWeightGram;
        list.WeightPercentage = weightResult.BodyWeightPercentage;
        list.WeightStatus = weightResult.WeightStatus;
        list.UpdatedAt = DateTime.Now;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ErrorResponse(
                "清單已被其他操作更新，請重新查詢後再試",
                statusCode: StatusCodes.Status409Conflict);
        }

        return CreatedResponse(
            new
            {
                list.ListId,
                Item = new PersonalEquipmentDetailItemDto
                {
                    DetailId = detail.DetailId,
                    EquipmentName = detail.CustomEquipmentName!,
                    Quantity = detail.Quantity,
                    UnitWeightGram = detail.UnitWeightGram,
                    TotalWeightGram = detail.TotalWeightGram,
                    RequirementLevel = detail.RequirementLevel,
                    IsPrepared = detail.IsPrepared,
                    Notes = detail.Notes
                },
                ListTotalWeightGram = list.TotalWeightGram,
                list.MaxCarryWeightGram,
                list.RemainingWeightGram,
                list.WeightPercentage,
                list.WeightStatus
            },
            "自訂裝備新增成功");
    }
    #endregion

}
