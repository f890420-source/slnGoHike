using NetTopologySuite.Geometries;

namespace prjGoHike.DTO.GoHikeSafe
{
    public class IndicatorDto
    {
        public long id { get; set; }

        public string IndicatorName { get; set; } = null!;

        public string IndicatorType { get; set; } = null!;

        public decimal Weight { get; set; }

        public byte IndicatorLevel { get; set; }

        public string? IndicatorDescription { get; set; }

        public string? DataSource { get; set; }

        public bool IsActive { get; set; }
    }
}
