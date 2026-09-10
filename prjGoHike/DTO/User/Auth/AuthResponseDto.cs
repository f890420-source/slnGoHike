namespace prjGoHike.DTO.Auth
{
    public class AuthResponseDto
    {/// 登入/註冊/換發 Token 成功後，統一回傳這個結構給前端
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }

    }
}
