using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using prjGoHike.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace prjGoHike.Services;

public sealed record EmailVerificationTicket(long UserId, string Email, string PasswordFingerprint);

public sealed class EmailVerificationTokenService(IDataProtectionProvider provider, IOptions<PasswordResetSettings> settings)
{
    private readonly ITimeLimitedDataProtector _protector = provider
        .CreateProtector("GoHike.EmailVerification.v1").ToTimeLimitedDataProtector();
    public string Create(User user) => _protector.Protect(
        JsonSerializer.Serialize(new EmailVerificationTicket(user.UserId, user.Email, Fingerprint(user.PasswordHash))),
        TimeSpan.FromMinutes(Math.Clamp(settings.Value.ExpiryMinutes, 5, 60)));

    public EmailVerificationTicket? Read(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return null;
        try { return JsonSerializer.Deserialize<EmailVerificationTicket>(_protector.Unprotect(token)); }
        catch (Exception exception) when (exception is CryptographicException or JsonException or FormatException) { return null; }
    }

    public bool Matches(EmailVerificationTicket ticket, User user) =>
        user.EmailVerifiedAt is null && ticket.UserId == user.UserId && ticket.Email == user.Email &&
        ticket.PasswordFingerprint == Fingerprint(user.PasswordHash);

    private static string Fingerprint(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
}
