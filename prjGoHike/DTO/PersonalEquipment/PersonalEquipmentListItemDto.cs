namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 「我的裝備清單」列表中，單筆清單的摘要資料。
/// 不包含逐項裝備；完整內容由明細 API 提供。
/// </summary>
public class PersonalEquipmentListItemDto
{
    /// <summary>清單 ID，供前端查詢該筆明細。</summary>
    public long ListId { get; set; }

    /// <summary>使用者自行輸入的清單名稱。</summary>
    public string ListName { get; set; } = "";

    /// <summary>關聯山岳目前的名稱，不一定與清單名稱相同。</summary>
    public string MountainName { get; set; } = "";

    /// <summary>清單記錄的登山日期，不包含時間。</summary>
    public DateOnly HikingDate { get; set; }

    /// <summary>登山天數。</summary>
    public int HikingDays { get; set; }

    /// <summary>清單儲存的裝備總重量，單位為公克。</summary>
    public int TotalWeightGram { get; set; }

    /// <summary>清單儲存的配重狀態文字。</summary>
    public string WeightStatus { get; set; } = "";

    /// <summary>清單建立時間。</summary>
    public DateTime CreatedAt { get; set; }
}
