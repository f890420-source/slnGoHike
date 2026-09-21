namespace prjGoHike.Services.PersonalEquipment;

/// <summary>
/// 裝備配重計算服務。
/// 根據裝備總重量及個人體重，計算參考負重上限、
/// 剩餘額度、重量比例及配重狀態。
/// 不負責登入驗證、資料庫查詢或儲存。
/// </summary>
public class EquipmentWeightService
{
    /// <summary>
    /// 沿用目前系統的計算規則，產生配重結果。
    /// 體重 20% 僅為系統參考規則，不代表個人安全保證。
    /// </summary>
    /// <param name="totalWeightGram">裝備總重量，單位：公克。</param>
    /// <param name="bodyWeightKg">個人體重，單位：公斤。</param>
    public EquipmentWeightResult Calculate(
        int totalWeightGram,
        decimal bodyWeightKg)
    {
        // 防止其他呼叫端傳入不合理數值。
        // 體重範圍與目前建立清單的 DTO 一致。
        if (totalWeightGram < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalWeightGram),
                "裝備總重量不可小於 0。");
        }

        if (bodyWeightKg < 20M || bodyWeightKg > 300M)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bodyWeightKg),
                "體重必須介於 20 至 300 公斤。");
        }

        // 將裝備重量轉成公斤，沿用體重 20% 的參考上限。
        decimal totalWeightKg = totalWeightGram / 1000M;
        decimal maxCarryWeightKg = bodyWeightKg * 0.2M;

        // 沿用原本的轉換方式，小數公克部分直接截去。
        int maxCarryWeightGram =
            decimal.ToInt32(maxCarryWeightKg * 1000M);

        // 超過參考上限時，剩餘額度顯示為 0。
        int remainingWeightGram =
            Math.Max(maxCarryWeightGram - totalWeightGram, 0);

        // 裝備重量占「個人體重」的比例。
        decimal bodyWeightPercentage = Math.Round(
            totalWeightKg / bodyWeightKg * 100M,
            2);

        // 裝備重量占「參考負重上限」的比例。
        decimal loadLimitUsagePercentage = Math.Round(
            totalWeightKg / maxCarryWeightKg * 100M,
            2);

        // 沿用原本的判斷順序與狀態文字。
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

        return new EquipmentWeightResult
        {
            TotalWeightGram = totalWeightGram,
            MaxCarryWeightGram = maxCarryWeightGram,
            RemainingWeightGram = remainingWeightGram,
            BodyWeightPercentage = bodyWeightPercentage,
            LoadLimitUsagePercentage = loadLimitUsagePercentage,
            WeightStatus = weightStatus
        };
    }
}
