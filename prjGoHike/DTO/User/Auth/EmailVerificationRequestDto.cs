using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.Auth;

public sealed class VerifyEmailRequestDto
{
    [Required, StringLength(4096)]
    public string Token { get; set; } = string.Empty;
}

public sealed class ResendVerificationRequestDto
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;
}
