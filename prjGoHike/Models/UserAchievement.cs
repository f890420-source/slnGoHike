using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class UserAchievement
{
    public long UserId { get; set; }

    public long AchievementId { get; set; }

    public DateTime UnlockedAt { get; set; }

    public virtual Achievement Achievement { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
