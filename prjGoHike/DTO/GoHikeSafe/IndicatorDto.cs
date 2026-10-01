using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace prjGoHike.DTO.GoHikeSafe
{
    public class IndicatorDto
    {
        public long id { get; set; }

        [Required, StringLength(120)]
        public string IndicatorName { get; set; } = null!;

        [Required, StringLength(30)]
        public string IndicatorType { get; set; } = null!;

        [Range(typeof(decimal), "0", "999.999")]
        public decimal Weight { get; set; }

        public byte? IndicatorLevel { get; set; }

        [StringLength(1500)]
        public string? IndicatorDescription { get; set; }

        [StringLength(500)]
        public string? DataSource { get; set; }

        public bool IsActive { get; set; }

        public List<IndicatorSegmentDto>? IndicatorSegments { get; set; }
    }

    public class IndicatorSegmentDto
    {
        public long id { get; set; }

        [StringLength(200)]
        public string? SegmentName { get; set; }

        [Required]
        public Geometry Shape { get; set; } = null!;

        [StringLength(100)]
        public string? SourceFeatureId { get; set; }

        public byte? SegmentLevel { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
