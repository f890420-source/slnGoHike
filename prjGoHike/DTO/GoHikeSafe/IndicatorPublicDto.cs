using NetTopologySuite.Geometries;

namespace prjGoHike.DTO.GoHikeSafe
{
    public class IndicatorTextInfoDto
    {
        public long id { get; set; }

        public string IndicatorName { get; set; } = null!;

        public string IndicatorType { get; set; } = null!;
    }
    
    public class IndicatorPublicDto
    {
        public long id { get; set; }

        public string IndicatorName { get; set; } = null!;

        public string IndicatorType { get; set; } = null!;

        public IEnumerable<IndicatorSegmentPublicDto>? IndiSegDtos { get; set; }
    }

    public class IndicatorSegmentPublicDto
    {
        public long id { get; set; }

        public string? SegmentName { get; set; }

        public Geometry Shape { get; set; } = null!;
    }
}
