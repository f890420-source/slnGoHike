using Microsoft.AspNetCore.Mvc;
using prjGoHike.APIControllers;
using prjGoHike.DTO.PersonalEquipment;
using prjGoHike.Services.PersonalEquipment;

namespace prjGoHike.APIControllers.PersonalEquipment;

/// <summary>
/// 裝備配重預覽 API。
/// 接收草稿總重量與個人體重，回傳配重分析。
/// 不讀取會員清單，也不建立、修改或儲存資料。
/// </summary>
[Route("api/v1/PersonalEquipmentApi")]
public class EquipmentWeightController : BaseController
{
    private readonly EquipmentWeightService _weightService;

    #region 建構子

    /// <summary>
    /// 注入現有配重服務，讓預覽與正式儲存沿用相同計算規則。
    /// </summary>
    public EquipmentWeightController(
        EquipmentWeightService weightService)
    {
        _weightService = weightService;
    }

    #endregion

    #region 預覽裝備配重

    /// <summary>
    /// 計算尚未儲存草稿的配重。
    /// 前端傳入的總重量只供預覽，
    /// 正式儲存清單時仍由後端依實際裝備重新計算。
    /// </summary>
    [HttpPost("weight-preview")]
    public IActionResult PreviewWeight(
        [FromBody] PreviewEquipmentWeightRequestDto request)
    {
        // BaseController 繼承的 ApiController 會先驗證 DTO。
        // 這裡也明確確認必要數值，避免使用缺少的資料。
        if (request.TotalWeightGram is not int totalWeightGram ||
            request.BodyWeightKg is not decimal bodyWeightKg)
        {
            return ErrorResponse("請提供草稿總重量與個人體重");
        }

        if (totalWeightGram < 0)
        {
            return ErrorResponse("草稿總重量不可小於 0");
        }

        if (bodyWeightKg < 20M || bodyWeightKg > 300M)
        {
            return ErrorResponse("個人體重必須介於 20 至 300 公斤");
        }

        // 配重公式集中在 Service，Controller 不重寫公式。
        var result = _weightService.Calculate(
            totalWeightGram,
            bodyWeightKg);

        // 明確選出 API 回傳欄位，不直接暴露內部結果物件。
        return SuccessResponse(
            new
            {
                BodyWeightKg = bodyWeightKg,
                result.TotalWeightGram,
                result.MaxCarryWeightGram,
                result.RemainingWeightGram,
                result.BodyWeightPercentage,
                result.LoadLimitUsagePercentage,
                result.WeightStatus
            },
            "取得配重預覽成功");
    }

    #endregion
}