using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Index.HPRtree;
using prjGoHike.APIControllers;
using prjGoHike.DTO.PersonalEquipment;
using prjGoHike.Models;
using prjGoHike.Services.PersonalEquipment;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace prjGoHike.APIControllers.PersonalEquipment;
/// <summary>
/// 會員個人裝備清單 API。
/// 負責建立、查詢清單與明細、修改名稱及軟刪除整張清單。
/// 裝備項目的異動由 PersonalEquipmentItemsController 負責。
/// 山岳選項與裝備建議由 EquipmentSuggestionsController 負責。
/// </summary>
/// <remarks>
/// 保留既有 API 路徑，避免影響前端呼叫。
/// 會員清單的操作必須驗證登入身分及資料擁有權。
/// </remarks>

[Route("api/v1/PersonalEquipmentApi")]
public class PersonalEquipmentListsController : BaseController
{
    private readonly GoHikeDataContext _db;
    private readonly EquipmentWeightService _weightService;

    /// <summary>
    /// 注入資料庫存取與配重計算服務。
    /// Controller 負責請求處理，計算公式交給服務。
    /// </summary>
    public PersonalEquipmentListsController(
        GoHikeDataContext db,
        EquipmentWeightService weightService)
    {
        _db = db;
        _weightService = weightService;
    }

    /// <summary>
    /// 從目前登入者的 Claims 取得會員 ID。
    /// 若沒有有效的會員 ID，回傳 null，由呼叫端拒絕操作。
    /// 不接受前端自行指定會員 ID 作為清單擁有者。
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

    #region 查詢我的裝備清單
    /// <summary>
    /// 查詢目前登入會員尚未刪除的裝備清單摘要。
    /// 依建立時間由新到舊排序，提供前端列表顯示。
    /// </summary>
    /// <remarks>
    /// GET /api/v1/PersonalEquipmentApi/lists
    /// 會員身分取自登入資訊，只回傳自己的清單。
    /// </remarks>
    [Authorize]
    [HttpGet("lists")]
    public async Task<IActionResult> GetMyLists(
    CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再查詢裝備清單",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var lists = await _db.PersonalEquipmentLists
            .AsNoTracking()
            .Where(list =>
                list.MemberId == currentUserId.Value &&
                !list.IsDeleted)
            .OrderByDescending(list => list.CreatedAt)
            .ThenByDescending(list => list.ListId)
            .Select(list => new PersonalEquipmentListItemDto
            {
                ListId = list.ListId,
                ListName = list.ListName,
                MountainName = list.Mountain.MountainName,
                HikingDate = list.HikingDate,
                HikingDays = list.HikingDays,
                TotalWeightGram = list.TotalWeightGram,
                WeightStatus = list.WeightStatus,
                CreatedAt = list.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return SuccessResponse(
            lists,
            "取得我的裝備清單成功");
    }
    #endregion

    #region 查詢清單明細
    /// <summary>
    /// 查詢自己單筆裝備清單的登山條件、重量摘要與裝備明細。
    /// 數量與重量讀取儲存紀錄，不重新計算裝備建議。
    /// </summary>
    /// <remarks>
    /// GET /api/v1/PersonalEquipmentApi/lists/{listId}
    /// 清單不存在、已刪除或不屬於目前會員時，皆回傳 404。
    /// </remarks>
    [Authorize]
    [HttpGet("lists/{listId:long}")]
    public async Task<IActionResult> GetListDetail(
    long listId,
    CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再查詢裝備清單",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var list = await _db.PersonalEquipmentLists
            .AsNoTracking()
            .Where(list =>
                list.ListId == listId &&
                list.MemberId == currentUserId.Value &&
                !list.IsDeleted)
            .Select(list => new PersonalEquipmentListDetailDto
            {
                ListId = list.ListId,
                ListName = list.ListName,
                MountainName = list.Mountain.MountainName,
                HikingDate = list.HikingDate,
                HikingDays = list.HikingDays,
                Season = list.Season,
                IntensityLevel = list.IntensityLevel,
                ExperienceLevel = list.ExperienceLevel,
                BodyWeightKg = list.BodyWeightKg,
                TotalWeightGram = list.TotalWeightGram,
                MaxCarryWeightGram = list.MaxCarryWeightGram,
                RemainingWeightGram = list.RemainingWeightGram,
                WeightStatus = list.WeightStatus,

                Items = list.PersonalEquipmentDetails
                    .OrderBy(detail => detail.SortOrder)
                    .ThenBy(detail => detail.DetailId)
                    .Select(detail => new PersonalEquipmentDetailItemDto
                    {
                        DetailId = detail.DetailId,
                        EquipmentName = detail.CustomEquipmentName
                            ?? (detail.Equipment != null
                                ? detail.Equipment.EquipmentName
                                : "未命名裝備"),
                        Quantity = detail.Quantity,
                        UnitWeightGram = detail.UnitWeightGram,
                        TotalWeightGram = detail.TotalWeightGram,
                        RequirementLevel = detail.RequirementLevel,
                        IsPrepared = detail.IsPrepared,
                        Notes = detail.Notes
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (list is null)
        {
            return NotFoundResponse("找不到指定的裝備清單");
        }

        return SuccessResponse(
            list,
            "取得裝備清單明細成功");
    }
    #endregion

    #region 修改清單名稱
    /// <summary>
    /// 修改目前登入會員自己的裝備清單名稱。
    /// 僅更新名稱與修改時間，不改變登山條件、裝備或配重結果。
    /// </summary>
    /// <remarks>
    /// PATCH /api/v1/PersonalEquipmentApi/lists/{listId}/name
    /// 清單不存在、已刪除或不屬於目前會員時，回傳 404。
    /// 儲存時偵測到並行修改衝突，回傳 409。
    /// </remarks>
    [Authorize]
    [HttpPatch("lists/{listId:long}/name")]
    public async Task<IActionResult> UpdateListName(
        long listId,
        [FromBody] UpdatePersonalEquipmentListNameRequestDto request,
        CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再修改清單名稱",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // 去除前後空白，再確認名稱有效。
        string normalizedName = request.ListName?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return ErrorResponse("請輸入清單名稱");
        }

        if (normalizedName.Length > 100)
        {
            return ErrorResponse("清單名稱最多 100 個字元");
        }

        // 只允許修改自己的清單，且排除已刪除的資料。
        // 本次不需要載入裝備明細。
        var list = await _db.PersonalEquipmentLists
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

        list.ListName = normalizedName;
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

        return SuccessResponse(
            new
            {
                list.ListId,
                list.ListName
            },
            "清單名稱更新成功");
    }
    #endregion

    #region 刪除裝備清單

    /// <summary>
    /// 將目前登入會員自己的清單標記為已刪除。
    /// 保留清單與裝備明細資料，不刪除全站共用裝備。
    /// </summary>
    /// <remarks>
    /// DELETE /api/v1/PersonalEquipmentApi/lists/{listId}
    /// 使用 IsDeleted 軟刪除，刪除後不再提供一般查詢與修改。
    /// 清單不存在、已刪除或不屬於目前會員時，回傳 404。
    /// </remarks>
    [Authorize]
    [HttpDelete("lists/{listId:long}")]
    public async Task<IActionResult> DeleteList(
        long listId,
        CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再刪除清單",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // 僅查詢目前會員自己的有效清單。
        // 本次不修改裝備明細，因此不需要 Include 明細資料。
        var list = await _db.PersonalEquipmentLists
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

        // 軟刪除：只更新標記與時間，不移除資料庫紀錄。
        list.IsDeleted = true;
        list.UpdatedAt = DateTime.Now;

        // 由清單的 RowVersion 偵測讀取後發生的並行修改。
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
                list.IsDeleted
            },
            "裝備清單刪除成功");
    }

    #endregion

    #region 檢查清單名稱是否重複

    /// <summary>
    /// 檢查目前會員是否已有同名的有效清單。
    /// 建立時檢查所有清單；改名時可排除目前這張清單。
    /// 僅供操作前提醒，不禁止同名，也不修改任何資料。
    /// </summary>
    [Authorize]
    [HttpGet("lists/name-exists")]
    public async Task<IActionResult> CheckListNameExists(
        [FromQuery] string? listName,
        [FromQuery] long? excludeListId,
        CancellationToken cancellationToken)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再檢查清單名稱",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        string normalizedName = listName?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(normalizedName) ||
            normalizedName.Length > 100)
        {
            return ErrorResponse("清單名稱必須為 1 至 100 個字元");
        }

        if (excludeListId.HasValue && excludeListId.Value <= 0)
        {
            return ErrorResponse("要排除的清單編號不正確");
        }

        // 僅查詢目前會員尚未刪除的清單。
        var query = _db.PersonalEquipmentLists
            .AsNoTracking()
            .Where(list =>
                list.MemberId == currentUserId.Value &&
                !list.IsDeleted &&
                list.ListName == normalizedName);

        // 改名時不把目前這張清單算進重複結果。
        // 這只影響查詢篩選，不代表授權修改該清單。
        if (excludeListId.HasValue)
        {
            long excludedId = excludeListId.Value;
            query = query.Where(list => list.ListId != excludedId);
        }

        bool exists = await query.AnyAsync(cancellationToken);

        return SuccessResponse(
            new
            {
                ListName = normalizedName,
                Exists = exists
            },
            "清單名稱檢查完成");
    }

    #endregion

    #region 建立裝備清單
    /// <summary>
    /// 為目前登入會員建立裝備清單、建議裝備及自訂裝備明細。
    /// 建議裝備重量取自資料庫，自訂裝備重量由輸入驗證後使用。
    /// 加總兩種裝備並計算配重後，將清單與所有明細一併儲存。
    /// </summary>
    /// <remarks>
    /// POST /api/v1/PersonalEquipmentApi/lists
    /// 清單擁有者取自登入資訊，不由前端指定。
    /// 體重 20% 為目前系統的配重參考規則，不代表個人安全保證。
    /// </remarks>
    [Authorize]
    [HttpPost("lists")]
    public async Task<IActionResult> CreateList(
    [FromBody] CreatePersonalEquipmentListRequestDto request)
    {
        long? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return ErrorResponse(
                "請先登入後再儲存裝備清單",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (request.HikingDate.Date < DateTime.Today)
        {
            return ErrorResponse("登山日期不可早於今天");
        }

        string[] validSeasons =
            ["春季", "夏季", "秋季", "冬季"];

        string[] validIntensityLevels =
            ["輕度", "中等", "高強度"];

        string[] validExperienceLevels =
            ["新手", "一般", "熟練"];

        if (!validSeasons.Contains(request.Season))
        {
            return ErrorResponse("登山季節不正確");
        }

        if (!validIntensityLevels.Contains(request.IntensityLevel))
        {
            return ErrorResponse("行程強度不正確");
        }

        if (!validExperienceLevels.Contains(request.ExperienceLevel))
        {
            return ErrorResponse("負重經驗不正確");
        }

        bool mountainExists = await _db.Mountains
            .AsNoTracking()
            .AnyAsync(mountain =>
                mountain.MountainId == request.MountainId);

        if (!mountainExists)
        {
            return NotFoundResponse("找不到指定的山岳");
        }

        List<long> suggestionIds = request.Items
    .Select(item => item.SuggestionId)
    .ToList();

        if (suggestionIds.Distinct().Count() != suggestionIds.Count)
        {
            return ErrorResponse("裝備建議不可重複");
        }

        var suggestions = await _db.MountainEquipmentSuggestions
            .AsNoTracking()
            .Where(suggestion =>
                suggestionIds.Contains(suggestion.SuggestionId) &&
                suggestion.MountainId == request.MountainId &&
                suggestion.MinimumDays <= request.HikingDays &&
                (
                    !suggestion.MaximumDays.HasValue ||
                    suggestion.MaximumDays.Value >= request.HikingDays
                ) &&
                (
                    suggestion.Season == "全年" ||
                    suggestion.Season == request.Season
                ) &&
                (
                    suggestion.IntensityLevel == null ||
                    suggestion.IntensityLevel == request.IntensityLevel
                ) &&
                (
                    suggestion.ExperienceLevel == null ||
                    suggestion.ExperienceLevel == request.ExperienceLevel
                ) &&
                suggestion.Equipment.IsActive)
            .Select(suggestion => new
            {
                suggestion.SuggestionId,
                suggestion.EquipmentId,
                suggestion.Equipment.StandardWeightGram,
                suggestion.RequirementLevel,
                suggestion.Notes
            })
            .ToListAsync();

        if (suggestions.Count != suggestionIds.Count)
        {
            return ErrorResponse(
                "部分裝備建議不存在、已停用或不符合目前登山條件");
        }

        long totalWeightGramValue = request.Items.Sum(item =>
        {
            var suggestion = suggestions.Single(
                value => value.SuggestionId == item.SuggestionId);

            return (long)suggestion.StandardWeightGram *
                item.Quantity;
        });

        // 先驗證自訂項目，建立尚未儲存的明細。
        // 後續與建議裝備一起加入清單，最後才統一儲存。
        var customDetails = new List<PersonalEquipmentDetail>();

        if (request.CustomItems is null)
        {
            return ErrorResponse("自訂裝備集合不可為 null");
        }

        foreach (var customItem in request.CustomItems)
        {
            if (customItem is null)
            {
                return ErrorResponse("自訂裝備項目不可為 null");
            }

            string name = customItem.CustomEquipmentName?.Trim() ?? "";
            string? notes = customItem.Notes?.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                return ErrorResponse("自訂裝備名稱必須為 1 至 100 個字元");
            }

            if (!customItem.UnitWeightGram.HasValue ||
                customItem.UnitWeightGram.Value < 1 ||
                customItem.UnitWeightGram.Value > 100000)
            {
                return ErrorResponse("自訂裝備單件重量必須介於 1 至 100000 公克");
            }

            if (!customItem.Quantity.HasValue ||
                customItem.Quantity.Value < 1 ||
                customItem.Quantity.Value > 20)
            {
                return ErrorResponse("自訂裝備數量必須介於 1 至 20");
            }

            if (notes is not null && notes.Length > 300)
            {
                return ErrorResponse("自訂裝備備註最多 300 個字元");
            }

            int unitWeightGram = customItem.UnitWeightGram.Value;
            int quantity = customItem.Quantity.Value;

            // 依上述範圍驗證，單項合計最多 2000000 公克。
            int itemTotalWeightGram = unitWeightGram * quantity;

            totalWeightGramValue += itemTotalWeightGram;

            if (totalWeightGramValue > int.MaxValue)
            {
                return ErrorResponse("裝備總重量超出允許範圍");
            }

            customDetails.Add(new PersonalEquipmentDetail
            {
                EquipmentId = null,
                CustomEquipmentName = name,
                UnitWeightGram = unitWeightGram,
                Quantity = quantity,
                TotalWeightGram = itemTotalWeightGram,
                RequirementLevel = null,
                IsPrepared = false,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes
            });
        }

        if (totalWeightGramValue > int.MaxValue)
        {
            return ErrorResponse("裝備總重量超出允許範圍");
        }

        // 總重量由已驗證的裝備建議與數量加總。
        // 配重公式及狀態判斷統一交給服務。
        var weightResult = _weightService.Calculate(
            (int)totalWeightGramValue,
            request.BodyWeightKg);

        bool memberExists = await _db.Users
        .AsNoTracking()
        .AnyAsync(user =>
        user.UserId == currentUserId.Value);

        if (!memberExists)
        {
            return NotFoundResponse("找不到目前登入的會員資料");
        }

        string normalizedListName =
            request.ListName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedListName))
        {
            return ErrorResponse("請輸入清單名稱");
        }

        PersonalEquipmentList list =
            new PersonalEquipmentList
            {
                MemberId = currentUserId.Value,
                MountainId = request.MountainId,
                ListName = normalizedListName,
                HikingDate = DateOnly.FromDateTime(
                    request.HikingDate),
                HikingDays = request.HikingDays,
                Season = request.Season,
                IntensityLevel = request.IntensityLevel,
                ExperienceLevel = request.ExperienceLevel,
                BodyWeightKg = request.BodyWeightKg,

                // 將配重服務的結果寫入清單。
                MaxCarryWeightGram = weightResult.MaxCarryWeightGram,
                TotalWeightGram = weightResult.TotalWeightGram,
                RemainingWeightGram = weightResult.RemainingWeightGram,
                WeightPercentage = weightResult.BodyWeightPercentage,
                WeightStatus = weightResult.WeightStatus,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

        for (int index = 0;
             index < request.Items.Count;
             index++)
        {
            CreatePersonalEquipmentItemDto requestItem =
                request.Items[index];

            var suggestion = suggestions.Single(value =>
                value.SuggestionId ==
                requestItem.SuggestionId);

            int itemTotalWeightGram =
                suggestion.StandardWeightGram *
                requestItem.Quantity;

            PersonalEquipmentDetail detail =
                new PersonalEquipmentDetail
                {
                    EquipmentId =
                        suggestion.EquipmentId,

                    Quantity =
                        requestItem.Quantity,

                    UnitWeightGram =
                        suggestion.StandardWeightGram,

                    TotalWeightGram =
                        itemTotalWeightGram,

                    RequirementLevel =
                        suggestion.RequirementLevel,

                    IsPrepared = false,

                    SortOrder = index + 1,

                    Notes = suggestion.Notes
                };

            list.PersonalEquipmentDetails.Add(detail);
        }

        // 建議裝備已在前面的迴圈加入。
        // 自訂裝備接續排在後面，與清單一起儲存。
        foreach (var customDetail in customDetails)
        {
            customDetail.SortOrder =
                list.PersonalEquipmentDetails.Count + 1;

            list.PersonalEquipmentDetails.Add(customDetail);
        }

        _db.PersonalEquipmentLists.Add(list);

        await _db.SaveChangesAsync();

        return CreatedResponse(
            new
            {
                list.ListId,
                list.ListName,
                list.TotalWeightGram,
                list.MaxCarryWeightGram,
                list.RemainingWeightGram,
                list.WeightPercentage,
                list.WeightStatus
            },
            "裝備清單儲存成功");
    }
    #endregion

}
