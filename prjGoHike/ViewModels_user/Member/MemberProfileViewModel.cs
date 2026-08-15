using System.ComponentModel.DataAnnotations;

namespace prjGoHike.ViewModels_user.Member
{
        public class MemberProfileViewModel
        {
            [Display(Name = "會員編號")]
            public long UserId { get; set; }

            [Display(Name = "名稱")]
            public string Nickname { get; set; }

            [Display(Name = "電子郵件")]
            public string Email { get; set; }

            [Display(Name = "個人簡介")]
            public string Bio { get; set; }

            [Display(Name = "頭貼網址")]
            public string AvatarUrl { get; set; }

            [Display(Name = "頭貼模糊狀態")]
            public string AvatarBlurState { get; set; }

            [Display(Name = "常去區域")]
            public string RegionPreference { get; set; }

            [Display(Name = "難度偏好")]
            public string DifficultyPreference { get; set; }

            [Display(Name = "註冊時間")]
            public DateTime CreatedAt { get; set; }

            [Display(Name = "最後活躍時間")]
            public DateTime LastActiveAt { get; set; }

            [Display(Name = "帳號狀態")]
            public string AccountStatus { get; set; }
        }
    public class MemberLevelViewModel
    {
        [Display(Name = "名稱")]
        public string Nickname { get; set; }

        [Display(Name = "目前等級")]
        public string LevelName { get; set; }

        [Display(Name = "目前等級編號")]
        public long CurrentLevelId { get; set; }

        [Display(Name = "累積經驗值")]
        public int TotalXp { get; set; }

        [Display(Name = "本級最低經驗值")]
        public int MinXp { get; set; }

        [Display(Name = "本級最高經驗值")]
        public int MaxXp { get; set; }

        [Display(Name = "升級進度百分比")]
        public decimal ProgressPercentage
        {
            get => MaxXp > MinXp ? (decimal)(TotalXp - MinXp) / (MaxXp - MinXp) * 100 : 0;
        }

        [Display(Name = "距離升級還需經驗值")]
        public int XpForNextLevel
        {
            get => Math.Max(0, MaxXp - TotalXp);
        }
    }
    public class MemberDashboardViewModel
    {
        // 基本資訊區塊
        [Display(Name = "暱稱")]
        public string Nickname { get; set; }

        [Display(Name = "頭貼網址")]
        public string AvatarUrl { get; set; }

        [Display(Name = "帳號狀態")]
        public string AccountStatus { get; set; }

        // 等級進度區塊
        [Display(Name = "目前等級")]
        public string LevelName { get; set; }

        [Display(Name = "累積經驗值")]
        public int TotalXp { get; set; }

        [Display(Name = "升級進度百分比")]
        public decimal ProgressPercentage { get; set; }

        // 成就區塊
        [Display(Name = "已解鎖成就")]
        public List<AchievementDto> UnlockedAchievements { get; set; } = new();

        [Display(Name = "成就總數")]
        public int TotalAchievementCount { get; set; }

        [Display(Name = "成就解鎖率")]
        public decimal AchievementPercentage
        {
            get => TotalAchievementCount > 0 ? (decimal)UnlockedAchievements.Count / TotalAchievementCount * 100 : 0;
        }

        // 標籤區塊
        [Display(Name = "擁有標籤")]
        public List<SkillTagDto> SkillTags { get; set; } = new();

        // 爬山紀錄區塊
        [Display(Name = "爬山紀錄")]
        public List<HikeRecordDto> HikeRecords { get; set; } = new();

        [Display(Name = "總爬山次數")]
        public int TotalHikeCount { get; set; }

        // 停權紀錄區塊(如果有的話)
        [Display(Name = "停權紀錄")]
        public List<SuspensionDto> SuspensionHistory { get; set; } = new();
    }

    // Dto 類別(用在 ViewModel 裡,簡化的資訊)
    public class AchievementDto
    {
        public long AchievementId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Rarity { get; set; }
        public DateTime UnlockedAt { get; set; }
    }

    public class SkillTagDto
    {
        public long TagId { get; set; }
        public string TagName { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }  // "系統自動" 或 "手動核發"
    }

    public class HikeRecordDto
    {
        public long RecordId { get; set; }
        public long MountainId { get; set; }
        public string MountainName { get; set; }
        public DateOnly HikeDate { get; set; }
        public int CompanionCount { get; set; }
        public bool Verified { get; set; }
    }

    public class SuspensionDto
    {
        public long SuspensionId { get; set; }
        public string Reason { get; set; }
        public DateTime SuspensionExpirationTime { get; set; }
        public string Status { get; set; }  // "停權中" 或 "已解除"
    }
}