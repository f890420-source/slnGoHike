using Microsoft.Extensions.Options;
using prjGoHike.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace prjGoHike.Services;

public interface IPasswordResetEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string email, string resetUrl, int expiryMinutes, CancellationToken cancellationToken);
}

public interface IEmailVerificationEmailSender
{
    bool IsConfigured { get; }
    Task SendVerificationAsync(string email, string verificationUrl, int expiryMinutes, CancellationToken cancellationToken);
}

public sealed class PasswordResetEmailSender(IOptions<EmailSettings> options) : IPasswordResetEmailSender, IEmailVerificationEmailSender
{
    private readonly EmailSettings _settings = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.Host) &&
        _settings.Port is > 0 and <= 65535 &&
        !string.IsNullOrWhiteSpace(_settings.Username) &&
        !string.IsNullOrWhiteSpace(_settings.Password) &&
        MailAddress.TryCreate(_settings.FromAddress, out _);

    public Task SendAsync(string email, string resetUrl, int expiryMinutes, CancellationToken cancellationToken) =>
        SendMessageAsync(email, "GoHike 密碼重設",
            $"你申請了 GoHike 密碼重設。\n\n請在 {expiryMinutes} 分鐘內開啟以下連結設定新密碼：\n{resetUrl}\n\n如果不是你提出申請，請忽略這封信，你的密碼不會變更。", cancellationToken);

    public Task SendVerificationAsync(string email, string verificationUrl, int expiryMinutes, CancellationToken cancellationToken) =>
        SendMessageAsync(email, "GoHike 信箱驗證",
            $"請驗證你的 GoHike 帳號信箱。\n\n請在 {expiryMinutes} 分鐘內開啟以下連結，並按下「驗證信箱」：\n{verificationUrl}\n\n驗證完成後才能使用密碼登入。若你沒有註冊或申請驗證，請忽略此信並勿點擊連結。", cancellationToken);

    private async Task SendMessageAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(email));
        // Google 顯示應用程式密碼時會以空格分組；其他 SMTP 的密碼保持原值。
        var smtpPassword = _settings.Host.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
            ? string.Concat(_settings.Password.Where(character => !char.IsWhiteSpace(character)))
            : _settings.Password;
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.Username, smtpPassword)
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        await client.SendMailAsync(message, timeout.Token);
    }
}
