using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 新增自訂裝備的請求資料。
/// 自訂裝備只加入會員自己的清單，不建立全站共用裝備。
/// 不接受前端指定會員 ID 或計算後的總重量。
/// </summary>
public class CreateCustomEquipmentRequestDto
{
    /// <summary>
    /// 自訂裝備名稱，必填且最多 100 個字元。
    /// 儲存前由後端去除前後空白。
    /// </summary>
    [Required(ErrorMessage = "請輸入裝備名稱")]
    [MaxLength(100, ErrorMessage = "裝備名稱最多 100 個字元")]
    public string CustomEquipmentName { get; set; } = "";

    /// <summary>
    /// 單件重量，單位為公克，使用整數。
    /// 目前功能限制為 1 至 100000 公克。
    /// </summary>
    [Required(ErrorMessage = "請輸入單件重量")]
    [Range(1, 100000, ErrorMessage = "單件重量必須介於 1 至 100000 公克")]
    public int? UnitWeightGram { get; set; }

    /// <summary>
    /// 攜帶數量，允許 1 至 20，與現有數量修改功能一致。
    /// </summary>
    [Required(ErrorMessage = "請輸入數量")]
    [Range(1, 20, ErrorMessage = "裝備數量必須介於 1 至 20")]
    public int? Quantity { get; set; }

    /// <summary>選填備註，最多 300 個字元。</summary>
    [MaxLength(300, ErrorMessage = "備註最多 300 個字元")]
    public string? Notes { get; set; }
}
