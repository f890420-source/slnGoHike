using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.Auth;
using prjGoHike.Models;
using prjGoHike.Services;

namespace prjGoHike.APIControllers.User;

[ApiController, AllowAnonymous, Route("api")]
[EnableRateLimiting("email-verification")]
public sealed class EmailVerificationController(GoHikeDataContext context, EmailVerificationService verification) : ControllerBase
{
    [HttpPost("verify-email")]
    public async Task<ActionResult> Verify(VerifyEmailRequestDto request, CancellationToken cancellationToken) =>
        await verification.ConfirmAsync(request.Token, cancellationToken)
            ? Ok(new { message = "信箱驗證成功，請使用帳號密碼登入。" })
            : BadRequest(new { message = "驗證連結無效、已過期或已使用，請重新寄送驗證信。" });

    [HttpPost("resend-verification")]
    public async Task<ActionResult> Resend(ResendVerificationRequestDto request, CancellationToken cancellationToken)
    {
        if (!verification.IsConfigured)
            return Problem("信箱驗證寄信服務尚未設定完成，請聯絡管理員。", statusCode: 503);
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is not null && user.AccountStatus == "正常" && user.EmailVerifiedAt is null &&
            user.PasswordHash.StartsWith("$2", StringComparison.Ordinal))
            await verification.SendAsync(user, cancellationToken);
        return Ok(new { message = "若此信箱有尚未驗證的會員帳號，將收到驗證信，請檢查收件匣與垃圾郵件。" });
    }
}
