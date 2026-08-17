using System.ComponentModel.DataAnnotations;

namespace prjGoHike.ViewModels_user.Member
{
    public class MemberAdminViewModel
    {
        public class MemberListItemViewModel
        {

            [Display(Name = "會員ID")]
            public long Id { get; set; }

            [Display(Name = "姓名")]
            public string Name { get; set; }

            [Display(Name = "信箱")]
            public string Email { get; set; }

            [Display(Name = "角色")]
            public string Role { get; set; }  // Member / EventLeader / Admin

            [Display(Name = "停權狀態")]
            public bool IsSuspended { get; set; }
        }

        public class MemberListViewModel
        {
            public List<MemberListItemViewModel> Items { get; set; } = new();
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public string? Search { get; set; }
            public string? RoleFilter { get; set; }
        }

        public class ChangeRoleViewModel
        {
            public long UserId { get; set; }
            public string Name { get; set; }
            public string CurrentRole { get; set; }
        }
    }
}
