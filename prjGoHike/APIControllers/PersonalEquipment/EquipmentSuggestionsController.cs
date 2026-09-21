using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers;
using prjGoHike.DTO.PersonalEquipment;
using prjGoHike.Models;

namespace prjGoHike.APIControllers.PersonalEquipment;

/// <summary>
/// 裝備建議 API。
/// 提供登山山岳選項，以及符合登山條件的裝備建議。
/// 不負責會員清單的新增、修改或儲存。
/// </summary>
/// <remarks>
/// 保留原本的 API 路徑，讓前端不需要更改呼叫網址。
/// </remarks>
[Route("api/v1/PersonalEquipmentApi")]
public class EquipmentSuggestionsController : BaseController
{
    private readonly GoHikeDataContext _db;

    public EquipmentSuggestionsController(GoHikeDataContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 取得建立裝備清單時可選擇的山岳。
    /// 依難度、海拔排序，回傳山岳基本資料及許可需求。
    /// </summary>
    [HttpGet("mountains")]
    public async Task<IActionResult> GetMountains()
    {
        var mountains = await _db.Mountains
            .AsNoTracking()
            .OrderBy(mountain => mountain.DifficultyLevel)
            .ThenBy(mountain => mountain.Altitude)
            .Select(mountain => new MountainOptionDto
            {
                MountainId = mountain.MountainId,
                MountainName = mountain.MountainName,
                Location = mountain.Location,
                Altitude = mountain.Altitude,
                DifficultyLevel = mountain.DifficultyLevel,
                MountainsPermitRequired = mountain.MountainsPermitRequired,
                NationalParkPermitRequired = mountain.NationalParkPermitRequired
            })
            .ToListAsync();

        return SuccessResponse(
            mountains,
            "取得山岳資料成功");
    }

    /// <summary>
    /// 根據山岳、登山天數、季節、行程強度及負重經驗，
    /// 查詢符合條件且裝備與分類皆啟用的裝備建議。
    /// 回傳建議數量、參考重量、重要程度及備註，不儲存會員清單。
    /// </summary>
    [HttpGet("equipment-suggestions")]
    public async Task<IActionResult> GetEquipmentSuggestions(
    [FromQuery] long mountainId,
    [FromQuery] string season,
    [FromQuery] string intensityLevel,
    [FromQuery] string experienceLevel,
    [FromQuery] int days = 1)
    {
        if (mountainId <= 0)
        {
            return ErrorResponse("請選擇山岳");
        }

        if (days <= 0)
        {
            return ErrorResponse("登山天數至少必須為 1 天");
        }

        var mountainExists = await _db.Mountains
            .AsNoTracking()
            .AnyAsync(mountain => mountain.MountainId == mountainId);

        if (!mountainExists)
        {
            return NotFoundResponse("找不到指定的山岳");
        }

        var suggestions = await _db.MountainEquipmentSuggestions
            .AsNoTracking()
            .Where(suggestion =>
                suggestion.MountainId == mountainId &&
                suggestion.MinimumDays <= days &&
                (
                    !suggestion.MaximumDays.HasValue ||
                    suggestion.MaximumDays.Value >= days
                ) &&
                (
                    suggestion.Season == "全年" ||
                    suggestion.Season == season
                ) &&
                (
                    suggestion.IntensityLevel == null ||
                    suggestion.IntensityLevel == intensityLevel
                ) &&
                (
                    suggestion.ExperienceLevel == null ||
                    suggestion.ExperienceLevel == experienceLevel
                ))

            .Where(suggestion =>
                suggestion.Equipment.IsActive &&
                suggestion.Equipment.Category.IsActive)
            .OrderBy(suggestion => suggestion.Equipment.Category.SortOrder)
            .ThenBy(suggestion =>
                suggestion.RequirementLevel == "必備" ? 0 : 1)
            .ThenBy(suggestion => suggestion.Equipment.EquipmentName)
            .Select(suggestion => new EquipmentSuggestionDto
            {
                SuggestionId = suggestion.SuggestionId,
                EquipmentId = suggestion.EquipmentId,
                CategoryName = suggestion.Equipment.Category.CategoryName,
                CategorySortOrder = suggestion.Equipment.Category.SortOrder,
                EquipmentName = suggestion.Equipment.EquipmentName,
                StandardWeightGram =
                    suggestion.Equipment.StandardWeightGram,
                SuggestedQuantity = suggestion.SuggestedQuantity,
                TotalWeightGram =
                    suggestion.Equipment.StandardWeightGram *
                    suggestion.SuggestedQuantity,
                RequirementLevel = suggestion.RequirementLevel,
                Description = suggestion.Equipment.Description,
                ImageUrl = suggestion.Equipment.ImageUrl,
                Notes = suggestion.Notes
            })
            .ToListAsync();

        return SuccessResponse(
            suggestions,
            "取得裝備建議成功");
    }
}
