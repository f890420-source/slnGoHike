namespace prjGoHike.ViewModels_user.Member
{
    public class MemberAdminViewModel
    {
        public class MemberListItemViewModel
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }       // Discriminator 值:Member / EventLeader / Admin
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
