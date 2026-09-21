namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 單筆裝備建議的回傳資料。
/// 由後端依登山條件篩選後，供前端顯示裝備建議表格。
/// 這是建議資料，不是已儲存的個人清單明細。
/// </summary>
public class EquipmentSuggestionDto
{
    /// <summary>
    /// 裝備建議紀錄的 ID。
    /// 建立清單時，前端使用此 ID 指定採用哪一筆建議。
    /// </summary>
    public long SuggestionId { get; set; }

    /// <summary>
    /// 裝備基本資料的 ID，與 SuggestionId 不同。
    /// 同一件裝備可以出現在不同的建議紀錄中。
    /// </summary>
    public long EquipmentId { get; set; }

    /// <summary>裝備分類名稱，例如衣著防護、照明安全。</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>裝備分類的排序值。</summary>
    public int CategorySortOrder { get; set; }

    /// <summary>裝備名稱。</summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>單件裝備的參考重量，單位為公克。</summary>
    public int StandardWeightGram { get; set; }

    /// <summary>符合目前登山條件的建議攜帶數量。</summary>
    public int SuggestedQuantity { get; set; }

    /// <summary>
    /// 單件參考重量乘以建議數量，單位為公克。
    /// 這是本項裝備的合計重量，不是整張清單總重量。
    /// </summary>
    public int TotalWeightGram { get; set; }

    /// <summary>這筆建議的重要程度，例如必備、建議。</summary>
    public string RequirementLevel { get; set; } = string.Empty;

    /// <summary>裝備說明，允許沒有內容。</summary>
    public string? Description { get; set; }

    /// <summary>裝備圖片網址，允許沒有圖片。</summary>
    public string? ImageUrl { get; set; }

    /// <summary>這筆裝備建議的補充備註。</summary>
    public string? Notes { get; set; }
}
