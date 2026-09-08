namespace prjGoHike.ViewModels_user.Member
{
    using System.ComponentModel.DataAnnotations;

 
 
        /// <summary>
        /// 登入表單 ViewModel
        /// </summary>
        public class LoginViewModel
        {
            [Required(ErrorMessage = "請輸入電子郵件")]
            [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
            [Display(Name = "電子郵件")]
            public string Email { get; set; }

            [Required(ErrorMessage = "請輸入密碼")]
            [DataType(DataType.Password)]
            [Display(Name = "密碼")]
            public string Password { get; set; }

            [Display(Name = "記住我")]
            public bool RememberMe { get; set; }

            /// <summary>
            /// 登入失敗錯誤訊息(由 Controller 填入)
            /// </summary>
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// 註冊表單 ViewModel（一般會員自助註冊用）
        /// </summary>
        public class RegisterViewModel
        {
            [Required(ErrorMessage = "請輸入暱稱")]
            [StringLength(20, MinimumLength = 2, ErrorMessage = "暱稱長度需在 2-20 字之間")]
            [Display(Name = "暱稱")]
            public string Nickname { get; set; }

            [Required(ErrorMessage = "請輸入電子郵件")]
            [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
            [Display(Name = "電子郵件")]
            public string Email { get; set; }

            [Required(ErrorMessage = "請輸入密碼")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度需至少 6 個字")]
            [DataType(DataType.Password)]
            [Display(Name = "密碼")]
            public string Password { get; set; }

            [Required(ErrorMessage = "請確認密碼")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "密碼不符，請重新檢查")]
            [Display(Name = "確認密碼")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "區域偏好（選填）")]
            public string RegionPreference { get; set; }

            [Display(Name = "難度偏好（選填）")]
            public string DifficultyPreference { get; set; }

            /// <summary>
            /// 註冊錯誤訊息(由 Controller 填入)
            /// </summary>
            public string? ErrorMessage { get; set; }
        }
    }

