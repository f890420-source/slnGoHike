using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class UserAchievements
{
    public long UserId { get; set; }

    public long AchievementId { get; set; }

    public DateTime UnlockedAt { get; set; }

    public virtual Achievements Achievement { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
