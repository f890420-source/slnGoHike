using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using prjGoHike.Services;
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

        [Range(1, 5, ErrorMessage = "指標等級必須介於 1～5。")]
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

        [Required, JsonConverter(typeof(GeoJsonGeometryConverter))]
        public Geometry Shape { get; set; } = null!;

        [StringLength(100)]
        public string? SourceFeatureId { get; set; }

        [Range(1, 5, ErrorMessage = "路段等級必須介於 1～5。")]
        public byte? SegmentLevel { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
