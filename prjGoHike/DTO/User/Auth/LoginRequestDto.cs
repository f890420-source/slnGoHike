using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.Auth

{  /// Angular 登入表單送過來的JSON
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; } = null!;

    }
}
