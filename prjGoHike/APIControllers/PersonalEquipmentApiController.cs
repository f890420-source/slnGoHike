using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.PersonalEquipment;
using prjGoHike.Models;

namespace prjGoHike.APIControllers;

public class PersonalEquipmentApiController : BaseController
{
    private readonly GoHikeDataContext _db;

    public PersonalEquipmentApiController(GoHikeDataContext db)
    {
        _db = db;
    }

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

    [HttpGet("equipment-suggestions")]
    public async Task<IActionResult> GetEquipmentSuggestions(
    [FromQuery] long mountainId,
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
