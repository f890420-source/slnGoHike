namespace prjGoHike.DTO.GroupJoinDTO
{
    public class EventRegistrationAndMemberListDTO
    {
        public long SignUpId { get; set; }

        public long UserId { get; set; }

        public long EventId { get; set; }

        public int RegistrationStatus { get; set; }

        public string EmergencyContact { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
