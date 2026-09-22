using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class RefreshTokens
{
    public long RefreshTokenId { get; set; }

    public long UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public virtual Users User { get; set; } = null!;
}
