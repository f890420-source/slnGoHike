using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 修改個人裝備清單名稱的請求資料。
/// 清單 ID 由 API 路徑提供，會員身分由登入資訊取得。
/// 不修改登山條件、裝備明細或配重結果。
/// </summary>
public class UpdatePersonalEquipmentListNameRequestDto
{
    /// <summary>
    /// 新的清單名稱，必填且最多 100 個字元。
    /// 儲存前由 Controller 去除前後空白。
    /// </summary>
    [Required(ErrorMessage = "請輸入清單名稱")]
    [MaxLength(100, ErrorMessage = "清單名稱最多 100 個字元")]
    public string ListName { get; set; } = "";
}
