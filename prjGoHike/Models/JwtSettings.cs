namespace prjGoHike.Models
{
    public class JwtSettings
    { 
       /// 對應 appsettings.json 的 "JwtSettings" 區塊。
       /// 用 Options Pattern 集中管理，如果要改金鑰只要改設定檔，不用動程式碼。
            public string SecretKey { get; set; } = null!;
            public string Issuer { get; set; } = null!;
            public string Audience { get; set; } = null!;
            public int AccessTokenExpiryMinutes { get; set; } = 30;
            public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}
