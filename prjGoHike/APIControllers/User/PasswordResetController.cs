using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using prjGoHike.DTO.Auth;
using prjGoHike.Models;
using prjGoHike.Services;
using System.Net.Mail;

namespace prjGoHike.APIControllers.User;

[ApiController]
[AllowAnonymous]
[Route("api")]
public sealed class PasswordResetController(
    GoHikeDataContext context,
    PasswordResetTokenService tokens,
    IPasswordHasher hasher,
    IPasswordResetEmailSender emailSender,
    IOptions<PasswordResetSettings> options,
    ILogger<PasswordResetController> logger) : ControllerBase
{
    private const string RequestMessage = "若此信箱已註冊且可使用密碼登入，將收到密碼重設信件，請檢查收件匣與垃圾郵件。使用 Google 註冊的帳號請直接使用 Google 登入。";
    private const string InvalidLinkMessage = "重設連結無效、已過期或已使用，請重新申請。";

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        // 設定錯誤對所有地址回覆相同結果，避免透露帳號是否存在。
        var frontendUrl = options.Value.FrontendUrl;
        if (!emailSender.IsConfigured || !Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontend) ||
            (frontend.Scheme != Uri.UriSchemeHttps &&
             !(frontend.Scheme == Uri.UriSchemeHttp && frontend.IsLoopback)) ||
            !string.IsNullOrEmpty(frontend.Query) || !string.IsNullOrEmpty(frontend.Fragment))
        {
            return Problem("密碼重設寄信服務尚未設定完成，請聯絡管理員。", statusCode: 503);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is not null && user.AccountStatus == "正常" && user.PasswordHash.StartsWith("$2", StringComparison.Ordinal))
        {
            // Fragment 不會出現在 HTTP 存取紀錄或 Referer 中。
            var url = $"{frontendUrl.TrimEnd('/')}/reset-password#token={Uri.EscapeDataString(tokens.Create(user))}";
            try
            {
                await emailSender.SendAsync(email, url, Math.Clamp(options.Value.ExpiryMinutes, 5, 60), cancellationToken);
            }
            catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException ||
                                               (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                // 不記錄憑證、信件內容或重設連結。
                if (exception is SmtpException smtpException)
                {
                    var reason = smtpException.Message.Contains("5.7.8", StringComparison.Ordinal) ||
                                 smtpException.Message.Contains("5.7.9", StringComparison.Ordinal) ||
                                 (int)smtpException.StatusCode == 535
                        ? "SMTP 驗證遭拒，請確認寄件帳號與有效的應用程式密碼。"
                        : "SMTP 伺服器拒絕寄信，請檢查寄件設定與郵件服務限制。";
                    logger.LogError("密碼重設信寄送失敗；SMTP 狀態碼：{SmtpStatusCode}，診斷：{Reason}",
                        (int)smtpException.StatusCode, reason);
                }
                else
                {
                    logger.LogError("密碼重設信寄送失敗；錯誤類型：{ErrorType}，診斷：{Reason}",
                        exception.GetType().Name,
                        exception is OperationCanceledException
                            ? "寄信連線超過 20 秒，請檢查網路及 SMTP 連接埠。"
                            : "寄信設定不正確，請檢查寄件地址與 SMTP 設定。");
                }
            }
        }

        return Ok(new { message = RequestMessage });
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var ticket = tokens.Read(request.Token);
        if (ticket is null) return BadRequest(new { message = InvalidLinkMessage });

        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == ticket.UserId, cancellationToken);
        if (user is null || user.AccountStatus != "正常" || !user.PasswordHash.StartsWith("$2", StringComparison.Ordinal) ||
            !tokens.Matches(ticket, user))
        {
            return BadRequest(new { message = InvalidLinkMessage });
        }

        var newHash = hasher.Hash(request.Password);
        var now = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // 條件更新保證兩個同時送出的重設請求只會有一個成功。
        var updated = await context.Users
            .Where(item => item.UserId == user.UserId && item.Email == ticket.Email &&
                           item.PasswordHash == user.PasswordHash && item.AccountStatus == "正常")
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.PasswordHash, newHash)
                .SetProperty(item => item.EmailVerifiedAt, item => item.EmailVerifiedAt ?? now), cancellationToken);
        if (updated != 1) return BadRequest(new { message = InvalidLinkMessage });

        await context.RefreshTokens.Where(token => token.UserId == user.UserId && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "密碼已重設，請使用新密碼登入。" });
    }
}
