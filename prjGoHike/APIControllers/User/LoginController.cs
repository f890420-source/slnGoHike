using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjGoHike.DTO.Auth;
using prjGoHike.Models;
using prjGoHike.Services;
using static prjGoHike.Models.UserPermissions;

namespace prjGoHike.APIControllers.User;

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
        request.Email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !IsBcryptHash(user.PasswordHash) ||
            !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("登入失敗：帳號或密碼錯誤");
            return Unauthorized(new { message = "帳號或密碼錯誤。" });
        }

        if (user.AccountStatus != "正常")
        {
            _logger.LogWarning("登入失敗：使用者 {UserId} 帳戶狀態異常", user.UserId);
            return Unauthorized(new { message = "帳戶目前無法登入。" });
        }

        var now = DateTime.UtcNow;
        var activeSuspension = await _context.SuspensionSchedules
            .AnyAsync(s => s.UserId == user.UserId && s.SuspensionExpirationTime > DateTime.Now, cancellationToken);

        if (activeSuspension)
        {
            _logger.LogWarning("登入失敗：使用者 {UserId} 處於停權狀態", user.UserId);
            return Unauthorized(new { message = "帳戶目前處於停權狀態。" });
        }

        user.LastActiveAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        var tokens = await _jwtTokenService.CreateTokenPairAsync(user, cancellationToken);
        _logger.LogInformation("使用者 {UserId} 登入成功", user.UserId);

        return Ok(ToAuthResponse(tokens));
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(
        RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        request.Email = request.Email.Trim().ToLowerInvariant();
        request.Nickname = request.Nickname.Trim();
        var exists = await _context.Users.AnyAsync(
            user => user.Email == request.Email || user.Nickname == request.Nickname,
            cancellationToken);

        if (exists)
            return Conflict(new { message = "電子郵件或暱稱已被使用。" });

        var defaultLevelId = await _context.Levels
            .OrderBy(level => level.MinXp)
            .Select(level => level.LevelId)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultLevelId == 0)
            return Problem("系統尚未設定初始會員等級，請聯絡管理員。", statusCode: StatusCodes.Status503ServiceUnavailable);

        var now = DateTime.UtcNow;
        var user = new Member
        {
            Nickname = request.Nickname,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "一般會員",
            AccountStatus = "正常",
            CurrentLevelId = defaultLevelId,
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

        return StatusCode(StatusCodes.Status201Created, new { message = "註冊成功。" });
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

    private static bool IsBcryptHash(string hash) => hash.StartsWith("$2", StringComparison.Ordinal);
}
