namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 單筆個人裝備清單的完整回傳資料。
/// 包含登山條件、儲存的配重摘要，以及逐項裝備明細。
/// 是否允許查閱，由 Controller 驗證會員身分及清單擁有權。
/// </summary>
public class PersonalEquipmentListDetailDto
{
    /// <summary>清單 ID。</summary>
    public long ListId { get; set; }

    /// <summary>使用者自行輸入的清單名稱。</summary>
    public string ListName { get; set; } = "";

    /// <summary>關聯山岳目前的名稱。</summary>
    public string MountainName { get; set; } = "";

    /// <summary>登山日期，不包含時間。</summary>
    public DateOnly HikingDate { get; set; }

    /// <summary>登山天數。</summary>
    public int HikingDays { get; set; }

    /// <summary>清單記錄的登山季節。</summary>
    public string Season { get; set; } = "";

    /// <summary>行程強度，允許未設定。</summary>
    public string? IntensityLevel { get; set; }

    /// <summary>負重經驗，允許未設定。</summary>
    public string? ExperienceLevel { get; set; }

    /// <summary>此清單用於計算配重的體重，單位為公斤。</summary>
    public decimal BodyWeightKg { get; set; }

    /// <summary>清單儲存的裝備總重量，單位為公克。</summary>
    public int TotalWeightGram { get; set; }

    /// <summary>
    /// 清單儲存的參考負重上限，單位為公克。
    /// 不代表個人安全保證。
    /// </summary>
    public int MaxCarryWeightGram { get; set; }

    /// <summary>清單儲存的剩餘負重額度，單位為公克。</summary>
    public int RemainingWeightGram { get; set; }

    /// <summary>清單儲存的配重狀態文字。</summary>
    public string WeightStatus { get; set; } = "";

    /// <summary>依排序值及明細 ID 排列的裝備明細。</summary>
    public List<PersonalEquipmentDetailItemDto> Items { get; set; } = [];
}

/// <summary>
/// 已儲存清單中的單筆裝備明細。
/// 數量與重量來自明細紀錄，不重新套用最新裝備建議。
/// </summary>
public class PersonalEquipmentDetailItemDto
{
    /// <summary>
    /// 清單明細 ID，更新準備狀態時使用。
    /// 不是裝備基本資料 ID，也不是裝備建議 ID。
    /// </summary>
    public long DetailId { get; set; }

    /// <summary>
    /// 優先使用自訂名稱，否則使用關聯裝備目前的名稱。
    /// 名稱不一定是建立清單當時的快照。
    /// </summary>
    public string EquipmentName { get; set; } = "";

    /// <summary>此項裝備儲存的攜帶數量。</summary>
    public int Quantity { get; set; }

    /// <summary>此項裝備儲存的單件重量，單位為公克。</summary>
    public int UnitWeightGram { get; set; }

    /// <summary>此項裝備儲存的合計重量，單位為公克。</summary>
    public int TotalWeightGram { get; set; }

    /// <summary>重要程度，例如必備、建議；允許未設定。</summary>
    public string? RequirementLevel { get; set; }

    /// <summary>true 表示已準備，false 表示未準備。</summary>
    public bool IsPrepared { get; set; }

    /// <summary>清單明細儲存的備註。</summary>
    public string? Notes { get; set; }
}
