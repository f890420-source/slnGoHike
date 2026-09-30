namespace prjGoHike.DTO.GroupJoinDTO
{
    public class EventDataUpdateDTO
    {
        public string? EventName { get; set; }
        public int MaximumNumber { get; set; }
        public DateTime EventStartTime { get; set; }
        public DateTime EventEndTime { get; set; }
        public string? Description { get; set; }
        public IFormFile? ActivityPhoto { get; set; }
    }
}
