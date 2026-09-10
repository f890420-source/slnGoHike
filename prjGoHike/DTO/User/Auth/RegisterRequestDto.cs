using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "請輸入暱稱")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "暱稱長度需在 2-20 字之間")]
        public string Nickname { get; set; } = null!;

        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "請輸入密碼")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度需至少 6 個字")]
        public string Password { get; set; } = null!;

        public string? RegionPreference { get; set; }
        public string? DifficultyPreference { get; set; }

        //驗證密碼一致放Angular
    }
}
