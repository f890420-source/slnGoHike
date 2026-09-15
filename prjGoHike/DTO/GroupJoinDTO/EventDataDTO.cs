namespace prjGoHike.DTO.GroupJoinDTO
{
    public class EventDataDTO
    {
        public long EventId { get; set; }
        public string? EventName { get; set; }
        public int MaximumNumber { get; set; }
        public string? ActivityStatus { get; set; }
        public IFormFile? ActivityPhoto { get; set; }
        //使用來自前端圖片的格式
        public string? Description { get; set; }
        public DateTime? EventStartTime { get; set; }
        public DateTime? EventEndTime { get; set; }
        public long MountainId { get; set; }
        public bool MountainsPermitRequired { get; set; }
        public bool NationalParkPermitRequired { get; set; }
        public int EventCount { get; set; }
        public int EveryMountainCount {  get; set; }
        public string? MountainName { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }

        public long LeaderUserId {  get; set; }
        //先用假資料做測試


    }
    
}
