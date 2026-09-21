using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.PersonalEquipment;
using prjGoHike.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using prjGoHike.APIControllers;
using prjGoHike.Services.PersonalEquipment;

namespace prjGoHike.APIControllers.PersonalEquipment;
/// <summary>
/// 會員個人裝備清單 API。
/// 負責建立清單、查詢清單摘要與明細，以及更新裝備準備狀態。
/// 山岳選項與裝備建議已移至 EquipmentSuggestionsController。
/// </summary>
/// <remarks>
/// 保留既有 API 路徑，避免影響前端呼叫與登入 Token 攔截器。
/// 會員清單的操作必須驗證登入身分及資料擁有權。
/// </remarks>
[Route("api/v1/PersonalEquipmentApi")]
public class PersonalEquipmentApiController : BaseController
{
    private readonly GoHikeDataContext _db;
    private readonly EquipmentWeightService _weightService;

    /// <summary>
    /// 注入資料庫存取與配重計算服務。
    /// Controller 負責請求處理，計算公式交給服務。
    /// </summary>
    public PersonalEquipmentApiController(
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

    /// <summary>
    /// 為目前登入會員建立裝備清單及其裝備明細。
    /// 驗證登山條件與裝備建議，依資料庫的參考重量及請求數量計算總重。
    /// 計算配重摘要後，將清單與明細一併儲存。
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

}
