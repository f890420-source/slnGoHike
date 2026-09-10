namespace prjGoHike.DTO.Auth
{
    public class RefreshRequestDto
    {
        /// <summary>換發 Token / 登出時，前端只需要把 Refresh Token 傳回來</summary>
            public string RefreshToken { get; set; } = null!;
    }
}
