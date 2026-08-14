using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Achievement
{
    public long AchievementId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Rarity { get; set; } = null!;

    public string ConditionType { get; set; } = null!;

    public string ConditionValue { get; set; } = null!;

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
