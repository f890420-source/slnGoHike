using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 建立個人裝備清單的請求資料。
/// 接收前端填寫的登山條件，以及選用的裝備建議與數量。
/// </summary>
/// <remarks>
/// 不接收會員 ID、單件重量或配重計算結果。
/// 會員身分由登入資訊取得，重量由後端查詢及計算。
/// </remarks>
public class CreatePersonalEquipmentListRequestDto
{
    /// <summary>
    /// 使用者自行命名的清單名稱，最多 100 個字元。
    /// 不必與山岳名稱相同。
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ListName { get; set; } = "";

    /// <summary>選擇的山岳 ID，是否存在由 Controller 驗證。</summary>
    [Range(1, long.MaxValue)]
    public long MountainId { get; set; }

    /// <summary>
    /// 登山日期；不可早於今天的規則由 Controller 驗證。
    /// </summary>
    [Required]
    public DateTime HikingDate { get; set; }

    /// <summary>登山天數，允許 1 至 30 天。</summary>
    [Range(1, 30)]
    public int HikingDays { get; set; }

    /// <summary>
    /// 登山季節；允許值由 Controller 驗證：
    /// 春季、夏季、秋季、冬季。
    /// </summary>
    [Required]
    public string Season { get; set; } = "";

    /// <summary>
    /// 行程強度；允許值由 Controller 驗證：
    /// 輕度、中等、高強度。
    /// </summary>
    [Required]
    public string IntensityLevel { get; set; } = "";

    /// <summary>
    /// 負重經驗；允許值由 Controller 驗證：
    /// 新手、一般、熟練。
    /// </summary>
    [Required]
    public string ExperienceLevel { get; set; } = "";

    /// <summary>
    /// 建立清單時的個人體重，單位為公斤，允許 20 至 300。
    /// 作為本次配重計算的輸入。
    /// </summary>
    [Range(20, 300)]
    public decimal BodyWeightKg { get; set; }

    /// <summary>
    /// 本次選用的裝備項目，集合至少包含一筆。
    /// 建議是否重複、是否符合登山條件，由 Controller 驗證。
    /// </summary>
    [MinLength(1)]
    public List<CreatePersonalEquipmentItemDto> Items { get; set; } = [];
}

/// <summary>
/// 建立清單時，單筆裝備項目的請求資料。
/// 後端依建議 ID 查出裝備及參考重量，不信任前端提供的重量。
/// </summary>
public class CreatePersonalEquipmentItemDto
{
    /// <summary>
    /// 裝備建議紀錄的 ID，不是裝備基本資料的 EquipmentId。
    /// </summary>
    [Range(1, long.MaxValue)]
    public long SuggestionId { get; set; }

    /// <summary>要加入清單的裝備數量，允許 1 至 20。</summary>
    [Range(1, 20)]
    public int Quantity { get; set; }
}
