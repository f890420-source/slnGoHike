using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.Auth;
using prjGoHike.Models;
using prjGoHike.Services;
using System.Security.Cryptography;
using System.Text;
using static prjGoHike.Models.UserPermissions;

namespace GoHike.Controllers;

[ApiController]
[Route("api")]
public class LoginController : ControllerBase
{
    private readonly GoHikeDataContext _context;
    private readonly ILogger<LoginController> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginController(
        GoHikeDataContext context,
        ILogger<LoginController> logger,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("登入失敗：帳號或密碼錯誤");
            return Unauthorized();
        }

        if (user.AccountStatus != "正常")
        {
            _logger.LogWarning("登入失敗：使用者 {UserId} 帳戶狀態異常", user.UserId);
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var activeSuspension = await _context.SuspensionSchedules
            .AnyAsync(s => s.UserId == user.UserId && s.SuspensionExpirationTime > DateTime.Now, cancellationToken);

        if (activeSuspension)
        {
            _logger.LogWarning("登入失敗：使用者 {UserId} 處於停權狀態", user.UserId);
            return Unauthorized();
        }

        // 只保留既有帳號的遷移路徑；所有新密碼皆使用 BCrypt。
        if (!IsBcryptHash(user.PasswordHash))
            user.PasswordHash = _passwordHasher.Hash(request.Password);

        user.LastActiveAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        var tokens = await _jwtTokenService.CreateTokenPairAsync(user, cancellationToken);
        _logger.LogInformation("使用者 {UserId} 登入成功", user.UserId);

        return Ok(ToAuthResponse(tokens));
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var exists = await _context.Users.AnyAsync(
            user => user.Email == request.Email || user.Nickname == request.Nickname,
            cancellationToken);

        if (exists)
            return Conflict(new { message = "電子郵件或暱稱已被使用。" });

        var now = DateTime.UtcNow;
        var user = new Member
        {
            Nickname = request.Nickname,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "一般會員",
            AccountStatus = "正常",
            CurrentLevelId = 1,
            TotalXp = 0,
            RegionPreference = request.RegionPreference ?? string.Empty,
            DifficultyPreference = request.DifficultyPreference ?? string.Empty,
            AvatarUrl = string.Empty,
            AvatarBlurState = "不模糊",
            Bio = string.Empty,
            CreatedAt = now,
            LastActiveAt = now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var tokens = await _jwtTokenService.CreateTokenPairAsync(user, cancellationToken);
        return Created(string.Empty, ToAuthResponse(tokens));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        RefreshRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized();

        var tokens = await _jwtTokenService.RotateRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (tokens is null)
            return Unauthorized();

        return Ok(ToAuthResponse(tokens));
    }

    private static AuthResponseDto ToAuthResponse(TokenPair tokens) => new()
    {
        AccessToken = tokens.AccessToken,
        RefreshToken = tokens.RefreshToken,
        ExpiresAt = tokens.ExpiresAt
    };

    private bool VerifyPassword(string password, string hash)
    {
        if (IsBcryptHash(hash))
            return _passwordHasher.Verify(password, hash);

        if (IsLegacySha256Hash(hash))
        {
            using var sha256 = SHA256.Create();
            return CryptographicOperations.FixedTimeEquals(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(password)),
                Convert.FromBase64String(hash));
        }

        // 僅供過去後台以明碼建立的帳號登入一次，成功後立即升級成 BCrypt。
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(hash));
    }

    private static bool IsBcryptHash(string hash) => hash.StartsWith("$2", StringComparison.Ordinal);

    private static bool IsLegacySha256Hash(string hash)
    {
        try
        {
            return Convert.FromBase64String(hash).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
