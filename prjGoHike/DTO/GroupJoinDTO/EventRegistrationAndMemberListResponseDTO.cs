namespace prjGoHike.DTO.GroupJoinDTO
{
    public class EventRegistrationAndMemberListResponseDTO
    {
        public long SignUpId { get; set; }

        public long UserId { get; set; }

        public long EventId { get; set; }

        public int RegistrationStatus { get; set; }

        public string EmergencyContact { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public string? AvatarBlurState { get; set; }

        public string? AvatarUrl { get; set; }
        public string Nickname { get; set; } = null!;
        public List<string> Skills { get; set; } = new();
    }
}
