using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.GoHikeSafe
{
    public class TrailDto
    {
        [Key]
        public long TrailId { get; set; }

        public string TrailName { get; set; } = null!;

        public string Region { get; set; } = null!;

        public int DifficultyLevel { get; set; }

        public decimal? DistanceKm { get; set; }

        public decimal? EstimatedHours { get; set; }

        public bool PermitRequired { get; set; }

        public bool GuideRequired { get; set; }

        public string? RegulationNote { get; set; }

        public bool IsPublished { get; set; }
        public Geometry? TrailPath { get; set; }
    }
}
