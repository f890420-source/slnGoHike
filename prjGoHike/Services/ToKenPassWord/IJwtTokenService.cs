using prjGoHike.Models;

namespace prjGoHike.Services
{
    public interface IJwtTokenService
    {
        Task<TokenPair> CreateTokenPairAsync(User user, CancellationToken cancellationToken = default);
        Task<TokenPair?> RotateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
