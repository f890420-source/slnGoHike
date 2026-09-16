using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using prjGoHike.DTO.Auth;
using prjGoHike.Models;
using prjGoHike.Services;
using System.Security.Cryptography;
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
    private readonly GoogleAuthSettings _googleAuthSettings;

    public LoginController(
        GoHikeDataContext context,
        ILogger<LoginController> logger,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOptions<GoogleAuthSettings> googleAuthSettings)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _googleAuthSettings = googleAuthSettings.Value;
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

    [HttpPost("login/google")]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin(
        GoogleLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_googleAuthSettings.ClientId))
        {
            _logger.LogError("Google 登入失敗：伺服器尚未設定 GoogleAuth:ClientId");
            return Problem(
                "Google 登入尚未完成伺服器設定。",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.Credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_googleAuthSettings.ClientId]
                });
        }
        catch (InvalidJwtException exception)
        {
            _logger.LogWarning(exception, "Google 登入失敗：無效的 ID Token");
            return Unauthorized(new { message = "Google 登入憑證無效或已過期。" });
        }

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
        {
            _logger.LogWarning("Google 登入失敗：Google 帳號沒有已驗證的電子郵件");
            return Unauthorized(new { message = "Google 帳號的電子郵件尚未通過驗證。" });
        }

        var email = payload.Email.Trim().ToLowerInvariant();
        var googleAvatarUrl = GetGoogleAvatarUrl(payload.Picture);
        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is null)
        {
            var defaultLevelId = await _context.Levels
                .OrderBy(level => level.MinXp)
                .Select(level => level.LevelId)
                .FirstOrDefaultAsync(cancellationToken);
            if (defaultLevelId == 0)
            {
                return Problem(
                    "系統尚未設定初始會員等級，請聯絡管理員。",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var now = DateTime.UtcNow;
            user = new Member
            {
                Nickname = await CreateAvailableNicknameAsync(payload.Name, email, cancellationToken),
                Email = email,
                // Google 登入會員沒有本機密碼；非 BCrypt 值也會被現有密碼登入流程拒絕。
                PasswordHash = $"GOOGLE_ONLY_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}",
                Role = "一般會員",
                AccountStatus = "正常",
                CurrentLevelId = defaultLevelId,
                TotalXp = 0,
                RegionPreference = string.Empty,
                DifficultyPreference = string.Empty,
                AvatarUrl = googleAvatarUrl,
                AvatarBlurState = "不模糊",
                Bio = string.Empty,
                CreatedAt = now,
                LastActiveAt = now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Google 帳號首次登入，已建立使用者 {UserId}", user.UserId);
        }
        else
        {
            var accountProblem = await GetAccountLoginProblemAsync(user, cancellationToken);
            if (accountProblem is not null)
            {
                _logger.LogWarning("Google 登入失敗：使用者 {UserId} 帳戶無法登入", user.UserId);
                return Unauthorized(new { message = accountProblem });
            }

            if (!string.IsNullOrWhiteSpace(googleAvatarUrl) &&
                (string.IsNullOrWhiteSpace(user.AvatarUrl) || IsGoogleAvatarUrl(user.AvatarUrl)))
            {
                user.AvatarUrl = googleAvatarUrl;
            }

            user.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var tokens = await _jwtTokenService.CreateTokenPairAsync(user, cancellationToken);
        _logger.LogInformation("使用者 {UserId} 使用 Google 登入成功", user.UserId);
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

    private async Task<string?> GetAccountLoginProblemAsync(
        prjGoHike.Models.User user,
        CancellationToken cancellationToken)
    {
        if (user.AccountStatus != "正常")
            return "帳戶目前無法登入。";

        var activeSuspension = await _context.SuspensionSchedules.AnyAsync(
            schedule => schedule.UserId == user.UserId &&
                        schedule.SuspensionExpirationTime > DateTime.Now,
            cancellationToken);

        return activeSuspension ? "帳戶目前處於停權狀態。" : null;
    }

    private async Task<string> CreateAvailableNicknameAsync(
        string? googleName,
        string email,
        CancellationToken cancellationToken)
    {
        const int maxLength = 20;
        var emailName = email.Split('@', 2)[0];
        var baseName = string.IsNullOrWhiteSpace(googleName) ? emailName : googleName.Trim();
        baseName = Truncate(baseName, maxLength);
        if (baseName.Length < 2)
            baseName = "Google山友";

        var candidate = baseName;
        var suffix = 1;
        while (await _context.Users.AnyAsync(user => user.Nickname == candidate, cancellationToken))
        {
            var suffixText = suffix++.ToString();
            candidate = $"{Truncate(baseName, maxLength - suffixText.Length)}{suffixText}";
        }

        return candidate;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string GetGoogleAvatarUrl(string? pictureUrl)
    {
        if (!Uri.TryCreate(pictureUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !IsGoogleAvatarUrl(uri.ToString()))
        {
            return string.Empty;
        }

        return Truncate(uri.ToString(), 255);
    }

    private static bool IsGoogleAvatarUrl(string avatarUrl)
    {
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri))
            return false;

        return uri.Host.Equals("googleusercontent.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase);
    }
}
