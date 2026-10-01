using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using prjGoHike.Services;
using NetTopologySuite.Geometries;

namespace prjGoHike.DTO.GoHikeSafe
{
    public class DisAlertDto
    {
        public long AlertId { get; set; }

        [Required, StringLength(30)]
        public string AlertType { get; set; } = null!;

        [Required, StringLength(180)]
        public string AlertTitle { get; set; } = null!;

        [StringLength(2000)]
        public string? AlertDescription { get; set; }

        [Range(1, 5, ErrorMessage = "警示嚴重等級必須介於 1～5。")]
        public byte SeverityLevel { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [StringLength(150)]
        public string? SourceAgency { get; set; }

        [StringLength(1000), Url]
        public string? SourceUrl { get; set; }

        public bool IsActive { get; set; }

        public List<AlertSegmentDto>? AlertSegments { get; set; }
    }

    public class AlertSegmentDto
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
