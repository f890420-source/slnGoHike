namespace prjGoHike.DTO.PersonalEquipment;

/// <summary>
/// 山岳選項的回傳資料。
/// 供前端登山地點下拉選單，以及選取後的山岳資訊卡使用。
/// 不代表會員已建立或儲存的裝備清單。
/// </summary>
public class MountainOptionDto
{
    /// <summary>山岳 ID，供前端查詢裝備建議及建立清單。</summary>
    public long MountainId { get; set; }

    /// <summary>山岳名稱。</summary>
    public string MountainName { get; set; } = string.Empty;

    /// <summary>山岳所在地區。</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>海拔高度，單位為公尺。</summary>
    public int Altitude { get; set; }

    /// <summary>資料庫設定的山岳難度等級。</summary>
    public int DifficultyLevel { get; set; }

    /// <summary>資料庫記錄是否需要入山許可。</summary>
    public bool MountainsPermitRequired { get; set; }

    /// <summary>資料庫記錄是否需要國家公園許可。</summary>
    public bool NationalParkPermitRequired { get; set; }
}
