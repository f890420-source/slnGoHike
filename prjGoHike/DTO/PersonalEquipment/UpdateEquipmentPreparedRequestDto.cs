using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 更新單件裝備準備狀態的請求資料。
/// 清單 ID 與明細 ID 由 API 路徑提供，會員身分由登入資訊取得。
/// </summary>
public class UpdateEquipmentPreparedRequestDto
{
    /// <summary>
    /// 要設定的準備狀態：true 為已準備，false 為未準備。
    /// 使用可空型別區分「未提供欄位」與「取消勾選」。
    /// null 會被 Required 驗證拒絕。
    /// </summary>
    [Required(ErrorMessage = "請提供裝備準備狀態")]
    public bool? IsPrepared { get; set; }
}
