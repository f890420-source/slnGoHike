using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 修改已儲存清單中，單筆裝備數量的請求資料。
/// 清單 ID 與明細 ID 由 API 路徑提供。
/// 會員身分由登入資訊取得，不接受前端指定擁有者。
/// </summary>
public class UpdateEquipmentQuantityRequestDto
{
    /// <summary>
    /// 要設定的裝備數量，允許 1 至 20。
    /// null 表示沒有提供數量，會被必填驗證拒絕。
    /// </summary>
    [Required(ErrorMessage = "請提供裝備數量")]
    [Range(1, 20, ErrorMessage = "裝備數量必須介於 1 至 20")]
    public int? Quantity { get; set; }
}
