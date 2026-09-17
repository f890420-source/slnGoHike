using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.PersonalEquipment;
using prjGoHike.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace prjGoHike.APIControllers;

public class PersonalEquipmentApiController : BaseController
{
    private readonly GoHikeDataContext _db;

    public PersonalEquipmentApiController(GoHikeDataContext db)
    {
        _db = db;
    }

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

        int totalWeightGram = (int)totalWeightGramValue;

        decimal totalWeightKg =
            totalWeightGram / 1000M;

        decimal maxCarryWeightKg =
            request.BodyWeightKg * 0.2M;

        int maxCarryWeightGram =
            decimal.ToInt32(maxCarryWeightKg * 1000M);

        int remainingWeightGram =
            Math.Max(
                maxCarryWeightGram - totalWeightGram,
                0);

        decimal bodyWeightPercentage =
            Math.Round(
                totalWeightKg /
                request.BodyWeightKg *
                100M,
                2);

        decimal loadLimitUsagePercentage =
            Math.Round(
                totalWeightKg /
                maxCarryWeightKg *
                100M,
                2);

        string weightStatus;

        if (totalWeightGram > maxCarryWeightGram)
        {
            weightStatus = "超過安全上限";
        }
        else if (loadLimitUsagePercentage >= 80M)
        {
            weightStatus = "接近安全上限";
        }
        else
        {
            weightStatus = "安全範圍";
        }

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
                MaxCarryWeightGram = maxCarryWeightGram,
                TotalWeightGram = totalWeightGram,
                RemainingWeightGram = remainingWeightGram,
                WeightPercentage = bodyWeightPercentage,
                WeightStatus = weightStatus,
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
