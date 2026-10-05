using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using prjGoHike.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace prjGoHike.Services;

public sealed record PasswordResetTicket(long UserId, string Email, string PasswordFingerprint);

public sealed class PasswordResetTokenService
{
    private readonly ITimeLimitedDataProtector _protector;
    private readonly PasswordResetSettings _settings;

    public PasswordResetTokenService(IDataProtectionProvider provider, IOptions<PasswordResetSettings> settings)
    {
        _protector = provider.CreateProtector("GoHike.PasswordReset.v1").ToTimeLimitedDataProtector();
        _settings = settings.Value;
    }

    public string Create(User user) => _protector.Protect(
        JsonSerializer.Serialize(new PasswordResetTicket(user.UserId, user.Email, Fingerprint(user.PasswordHash))),
        TimeSpan.FromMinutes(Math.Clamp(_settings.ExpiryMinutes, 5, 60)));

    public PasswordResetTicket? Read(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return null;
        try
        {
            return JsonSerializer.Deserialize<PasswordResetTicket>(_protector.Unprotect(token));
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or FormatException)
        {
            return null;
        }
    }

    // 更換密碼後，所有先前寄出的重設連結會一起失效。
    public bool Matches(PasswordResetTicket ticket, User user) =>
        ticket.UserId == user.UserId && ticket.Email == user.Email &&
        ticket.PasswordFingerprint == Fingerprint(user.PasswordHash);

    private static string Fingerprint(string hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
}
