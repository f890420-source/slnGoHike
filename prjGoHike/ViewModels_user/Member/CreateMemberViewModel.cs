using System.ComponentModel.DataAnnotations;

namespace prjGoHike.ViewModels_user.Member
{
    public class CreateMemberViewModel
    {
        [Required(ErrorMessage = "暱稱必填")]
        [Display(Name = "暱稱")]
        public string Nickname { get; set; }

        [Required(ErrorMessage = "電子郵件必填")]
        [EmailAddress(ErrorMessage = "郵件格式錯誤")]
        [Display(Name = "電子郵件")]
        public string Email { get; set; }

        [Required(ErrorMessage = "密碼必填")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼至少 6 位")]
        [Display(Name = "密碼")]
        public string PasswordHash { get; set; }

        [Display(Name = "區域偏好")]
        public string RegionPreference { get; set; }

        [Display(Name = "難度偏好")]
        public string DifficultyPreference { get; set; }
    }
}
