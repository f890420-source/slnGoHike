using NetTopologySuite.Geometries;

namespace prjGoHike.DTO.GoHikeSafe
{
    public class TrailPublicDto
    {
        public long id { get; set; }

        public string TrailName { get; set; } = null!;
        public string Region { get; set; } = null!;

        public int DifficultyLevel { get; set; }

        public decimal? DistanceKm { get; set; }
        public IEnumerable<TrailSegmentPublicDto>? TrailSegDtos { get; set; }
    }

    public class TrailSegmentPublicDto
    {
        public long id { get; set; }

        public string? Source { get; set; }

        public required Geometry Geometry { get; set; }
    }
}
