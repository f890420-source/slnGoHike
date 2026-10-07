using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.Auth;

public sealed class ForgotPasswordRequestDto
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequestDto
{
    [Required, StringLength(4096)]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "兩次輸入的密碼不一致。")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
