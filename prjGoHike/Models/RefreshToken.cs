using System;
using System.Collections.Generic;


namespace prjGoHike.Models
{
    public class RefreshToken
    {
       
        ///  Access Token 效期短
        ///  Refresh Token 存進資料庫，可隨時撤銷
        ///  每次 Refresh 都做 Token Rotation，舊的立刻作廢、換發新的
            public long RefreshTokenId { get; set; }

            public long UserId { get; set; }

            public string Token { get; set; } = null!;

            public DateTime ExpiresAt { get; set; }

            public DateTime CreatedAt { get; set; }

            public DateTime? RevokedAt { get; set; }

            public string? ReplacedByToken { get; set; }

            public virtual User User { get; set; } = null!;

            /// <summary>這個 Token 現在是否還能用</summary>
            public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
        }
    }
