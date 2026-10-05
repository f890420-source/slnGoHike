using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using prjGoHike.DTO.Auth;
using prjGoHike.Services;

namespace prjGoHike.APIControllers.User;

[ApiController, AllowAnonymous, Route("api")]
[EnableRateLimiting("email-verification")]
public sealed class EmailVerificationController(EmailVerificationService verification) : ControllerBase
{
    [HttpPost("verify-email")]
    public async Task<ActionResult> Verify(VerifyEmailRequestDto request, CancellationToken cancellationToken) =>
        await verification.ConfirmAsync(request.Token, cancellationToken)
            ? Ok(new { message = "信箱驗證成功，請使用帳號密碼登入。" })
            : BadRequest(new { message = "驗證連結無效、已過期或已使用。若尚未完成驗證，請聯絡管理員。" });
}
