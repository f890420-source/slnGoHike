using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Achievements
{
    public long AchievementId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Rarity { get; set; } = null!;

    public string ConditionType { get; set; } = null!;

    public string ConditionValue { get; set; } = null!;

    public virtual ICollection<UserAchievements> UserAchievements { get; set; } = new List<UserAchievements>();

    public virtual ICollection<Users> Users { get; set; } = new List<Users>();
}
