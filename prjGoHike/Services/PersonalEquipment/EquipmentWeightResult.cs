namespace prjGoHike.Services.PersonalEquipment;

/// <summary>
/// 配重計算結果，供 Controller 儲存清單時使用。
/// 屬於程式內部資料，不是資料表或 API 回傳 DTO。
/// </summary>
public class EquipmentWeightResult
{
    /// <summary>裝備總重量，單位：公克。</summary>
    public int TotalWeightGram { get; init; }

    /// <summary>
    /// 依體重 20% 計算的參考負重上限，單位：公克。
    /// 不代表個人安全保證。
    /// </summary>
    public int MaxCarryWeightGram { get; init; }

    /// <summary>剩餘負重額度，最低為 0 公克。</summary>
    public int RemainingWeightGram { get; init; }

    /// <summary>
    /// 裝備重量占體重的百分比，對應資料表 WeightPercentage。
    /// </summary>
    public decimal BodyWeightPercentage { get; init; }

    /// <summary>裝備重量占參考負重上限的百分比。</summary>
    public decimal LoadLimitUsagePercentage { get; init; }

    /// <summary>沿用系統既有的配重狀態文字。</summary>
    public string WeightStatus { get; init; } = "";
}
