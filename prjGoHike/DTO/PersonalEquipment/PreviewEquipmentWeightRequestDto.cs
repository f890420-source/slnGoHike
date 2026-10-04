using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 預覽尚未儲存的裝備草稿配重。
/// 僅接收草稿總重量與個人體重，不建立或修改任何清單。
/// 正式儲存時，後端仍會依實際裝備資料重新計算重量。
/// </summary>
public class PreviewEquipmentWeightRequestDto
{
    #region 草稿總重量

    /// <summary>
    /// 前端加總的草稿重量，單位為公克。
    /// 僅供預覽，不作為正式儲存時的可信重量來源。
    /// </summary>
    [Required(ErrorMessage = "請提供草稿總重量")]
    [Range(0, int.MaxValue, ErrorMessage = "草稿總重量必須是非負整數且不可超過系統上限")]
    public int? TotalWeightGram { get; set; }

    #endregion

    #region 個人體重

    /// <summary>
    /// 本次行程使用的個人體重，單位為公斤。
    /// 與建立清單的體重限制一致。
    /// </summary>
    [Required(ErrorMessage = "請提供個人體重")]
    [Range(20, 300, ErrorMessage = "個人體重必須介於 20 至 300 公斤")]
    public decimal? BodyWeightKg { get; set; }

    #endregion
}
