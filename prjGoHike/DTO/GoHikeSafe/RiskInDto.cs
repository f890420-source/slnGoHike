using NetTopologySuite.Geometries;

namespace prjGoHike.DTO.GoHikeSafe
{
    public class RiskInDto
    {
        public long RiskIndicatorId { get; set; }

        public string IndicatorName { get; set; } = null!;

        public string IndicatorType { get; set; } = null!;

        public decimal Weight { get; set; }

        public byte RiskLevel { get; set; }

        public string? IndicatorDescription { get; set; }

        public Geometry? SpatialArea { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public string? DataSource { get; set; }

        public bool IsActive { get; set; }
    }
}
