namespace prjGoHike.DTO.GroupJoinDTO
{
    public class EventDataDTO
    {
        public long EventId { get; set; }
        public string? EventName { get; set; }
        public int MaximumNumber { get; set; }
        public string? ActivityStatus { get; set; }
        public string? ActivityPhoto { get; set; }
        public string? Description { get; set; }
        public DateTime? EventStartTime { get; set; }
        public DateTime? EventEndTime { get; set; }
        public long MountainId { get; set; }
        public bool MountainsPermitRequired { get; set; }
        public bool NationalParkPermitRequired { get; set; }

    }
}
