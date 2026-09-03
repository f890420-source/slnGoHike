using Microsoft.Identity.Client;

namespace prjGoHike.Models.Dtos
{
    public class MountainDTO
    {
        public long MountainId { get; set; }
        public string? MountainName { get; set; }
        public string? Location {  get; set; }
        public int Altitude { get; set; }
        public int DifficultyLevel { get; set; }
        public bool MountainsPermitRequired { get; set; }
        public bool NationalParkPermitRequired { get; set; }
    }
}
