using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.Auth;

public sealed class VerifyEmailRequestDto
{
    [Required, StringLength(4096)]
    public string Token { get; set; } = string.Empty;
}
