using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.User;

public sealed class UpdateProfileRequest
{
    [Required, StringLength(20, MinimumLength = 2)]
    public string Nickname { get; set; } = string.Empty;

    [StringLength(500)] public string? Bio { get; set; }
    [StringLength(255)] public string? AvatarUrl { get; set; }
    [StringLength(20)] public string? AvatarBlurState { get; set; }
    [StringLength(100)] public string? RegionPreference { get; set; }
    [StringLength(50)] public string? DifficultyPreference { get; set; }
}

public sealed class ChangeRoleRequest
{
    [Required] public string Role { get; set; } = string.Empty;
}

public sealed class ChangeAccountStatusRequest
{
    [Required, StringLength(20)] public string AccountStatus { get; set; } = string.Empty;
}

public sealed class LevelRequest
{
    [Required, StringLength(50)] public string LevelName { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int MinXp { get; set; }
    [Range(0, int.MaxValue)] public int MaxXp { get; set; }
}

public sealed class GrantXpRequest
{
    [Range(1, 1_000_000)] public int Amount { get; set; }
}

public sealed class AchievementRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(255)] public string Description { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Rarity { get; set; } = string.Empty;
    [Required, StringLength(50)] public string ConditionType { get; set; } = string.Empty;
    [Required, StringLength(100)] public string ConditionValue { get; set; } = string.Empty;
}

public sealed class SkillTagRequest
{
    [Required, StringLength(20)] public string Category { get; set; } = string.Empty;
    [Required, StringLength(50)] public string TagName { get; set; } = string.Empty;
    public long? ParentTagId { get; set; }
    [StringLength(50)] public string? UnlockCondition { get; set; }
}

public sealed class AssignSkillTagRequest
{
    [StringLength(20)] public string? Source { get; set; }
}

public sealed class SkillTagTreeNodeDto
{
    public long TagId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public long? ParentTagId { get; set; }
    public string? UnlockCondition { get; set; }
    public List<SkillTagTreeNodeDto> Children { get; set; } = [];
}

public sealed class UserAchievementDto
{
    public long UserId { get; set; }
    public long AchievementId { get; set; }
    public DateTime UnlockedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public string ConditionValue { get; set; } = string.Empty;
}

public sealed class UserSkillTagDto
{
    public long UserId { get; set; }
    public long TagId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public long? ParentTagId { get; set; }
    public string UnlockCondition { get; set; } = string.Empty;
}
