using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using prjGoHike.Models;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace prjGoHike.Services;

public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly GoHikeDataContext _context;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings, GoHikeDataContext context)
    {
        _jwtSettings = jwtSettings.Value;
        _context = context;
    }

    public async Task<TokenPair> CreateTokenPairAsync(User user, CancellationToken cancellationToken = default)
    {
        var rawRefreshToken = GenerateRefreshToken();
        var now = DateTime.UtcNow;
        var refreshToken = new RefreshToken
        {
            UserId = user.UserId,
            Token = HashRefreshToken(rawRefreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return CreateTokenPair(user, rawRefreshToken, now);
    }

    public async Task<TokenPair?> RotateRefreshTokenAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var oldTokenHash = HashRefreshToken(rawRefreshToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var oldRefreshToken = await _context.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.Token == oldTokenHash, cancellationToken);

        var isSuspended = oldRefreshToken is not null && await _context.SuspensionSchedules
            .AnyAsync(schedule =>
                schedule.UserId == oldRefreshToken.UserId &&
                schedule.SuspensionExpirationTime > DateTime.Now,
                cancellationToken);

        if (oldRefreshToken is null ||
            !oldRefreshToken.IsActive ||
            oldRefreshToken.User.AccountStatus != "正常" ||
            isSuspended)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var now = DateTime.UtcNow;
        var newRawRefreshToken = GenerateRefreshToken();
        var newRefreshTokenHash = HashRefreshToken(newRawRefreshToken);

        oldRefreshToken.RevokedAt = now;
        oldRefreshToken.ReplacedByToken = newRefreshTokenHash;
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = oldRefreshToken.UserId,
            Token = newRefreshTokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateTokenPair(oldRefreshToken.User, newRawRefreshToken, now);
    }

    private TokenPair CreateTokenPair(User user, string rawRefreshToken, DateTime issuedAt)
    {
        var expiresAt = issuedAt.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);
        return new TokenPair(
            GenerateAccessToken(user, issuedAt, expiresAt),
            rawRefreshToken,
            expiresAt);
    }

    private string GenerateAccessToken(User user, DateTime issuedAt, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Nickname),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, ToAuthorizationRole(user.Role)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    // 資料庫只保存雜湊，原始 Refresh Token 僅在簽發或換發當下回傳一次。
    private static string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private static string ToAuthorizationRole(string role) => role switch
    {
        "管理員" or "Admin" => "Admin",
        "團主" or "EventLeader" => "EventLeader",
        _ => "Member"
    };
}
