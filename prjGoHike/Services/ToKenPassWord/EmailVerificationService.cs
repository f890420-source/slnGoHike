using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using prjGoHike.Models;
using System.Net.Mail;
using System.Security.Cryptography;

namespace prjGoHike.Services;

public sealed class EmailVerificationService(
    GoHikeDataContext context,
    EmailVerificationTokenService tokens,
    IEmailVerificationEmailSender sender,
    IOptions<PasswordResetSettings> settings,
    ILogger<EmailVerificationService> logger)
{
    public bool IsConfigured => sender.IsConfigured &&
        Uri.TryCreate(settings.Value.FrontendUrl, UriKind.Absolute, out var url) &&
        (url.Scheme == Uri.UriSchemeHttps || (url.Scheme == Uri.UriSchemeHttp && url.IsLoopback)) &&
        string.IsNullOrEmpty(url.Query) && string.IsNullOrEmpty(url.Fragment);

    public async Task<bool> SendAsync(User user, CancellationToken cancellationToken)
    {
        if (!IsConfigured) return false;
        var url = $"{settings.Value.FrontendUrl.TrimEnd('/')}/verify-email#token={Uri.EscapeDataString(tokens.Create(user))}";
        try
        {
            await sender.SendVerificationAsync(user.Email, url, Math.Clamp(settings.Value.ExpiryMinutes, 5, 60), cancellationToken);
            logger.LogInformation("信箱驗證信已交付 SMTP，會員 {UserId}", user.UserId);
            return true;
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException ||
                                           (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogError("信箱驗證信寄送失敗；錯誤類型：{ErrorType}，SMTP 狀態碼：{SmtpStatusCode}",
                exception.GetType().Name, (exception as SmtpException)?.StatusCode);
            return false;
        }
    }

    public async Task<bool> ConfirmAsync(string token, CancellationToken cancellationToken)
    {
        var ticket = tokens.Read(token);
        if (ticket is null) return false;
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == ticket.UserId, cancellationToken);
        if (user is null || user.AccountStatus != "正常" || !tokens.Matches(ticket, user)) return false;
        var now = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var updated = await context.Users.Where(item => item.UserId == ticket.UserId && item.Email == ticket.Email &&
                item.PasswordHash == user.PasswordHash && item.EmailVerifiedAt == null && item.AccountStatus == "正常")
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.EmailVerifiedAt, now), cancellationToken);
        if (updated != 1) return false;
        await RevokeRefreshTokensAsync(user.UserId, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task MarkGoogleVerifiedAsync(User user, CancellationToken cancellationToken)
    {
        if (user.EmailVerifiedAt is not null) return;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // 未驗證帳號的密碼可能由他人預先註冊；Google 證明信箱所有權後清除該密碼。
        user.PasswordHash = $"GOOGLE_ONLY_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
        user.EmailVerifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await RevokeRefreshTokensAsync(user.UserId, user.EmailVerifiedAt.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private Task<int> RevokeRefreshTokensAsync(long userId, DateTime now, CancellationToken cancellationToken) =>
        context.RefreshTokens.Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
}
