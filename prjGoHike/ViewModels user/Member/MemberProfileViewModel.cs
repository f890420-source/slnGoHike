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
    }