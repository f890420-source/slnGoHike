using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.Auth;

public class GoogleLoginRequestDto
{
    [Required(ErrorMessage = "缺少 Google 登入憑證。")]
    public string Credential { get; set; } = null!;
}
