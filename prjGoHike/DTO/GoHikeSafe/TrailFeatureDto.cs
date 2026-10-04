using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;
using prjGoHike.Services;

namespace prjGoHike.DTO.GoHikeSafe;

public class TrailFeatureReportRequestDto
{
    [Required, Range(1, long.MaxValue)]
    public long TrailId { get; set; }

    [Required, StringLength(20), RegularExpression(@"^[A-Za-z][A-Za-z0-9_-]{0,19}$")]
    public string FeatureType { get; set; } = null!;

    [Required, StringLength(120)]
    public string FeatureName { get; set; } = null!;

    [Required, JsonConverter(typeof(GeoJsonPointConverter))]
    public Point Location { get; set; } = null!;

    [StringLength(1000)]
    public string? FeatureDescription { get; set; }
}

public class TrailFeatureUpdateRequestDto : TrailFeatureReportRequestDto
{
    [Required, Range(1, 5, ErrorMessage = "可信度必須介於 1～5。")]
    public byte ReliabilityLevel { get; set; }

    [Required]
    public bool? IsAvailable { get; set; }

    [StringLength(200)]
    public string? DataSource { get; set; }
}

public class TrailFeatureDto
{
    public long FeatureId { get; set; }
    public long TrailId { get; set; }
    public string FeatureType { get; set; } = null!;
    public string FeatureName { get; set; } = null!;

    [JsonConverter(typeof(GeoJsonGeometryConverter))]
    public Geometry Location { get; set; } = null!;

    public string? FeatureDescription { get; set; }
    public byte ReliabilityLevel { get; set; }
    public bool IsAvailable { get; set; }
    public string? DataSource { get; set; }
}

public class TrailFeatureQueryDto
{
    [Range(1, long.MaxValue)]
    public long? TrailId { get; set; }

    [StringLength(20), RegularExpression(@"^[A-Za-z][A-Za-z0-9_-]{0,19}$")]
    public string? FeatureType { get; set; }
}

public class TrailFeatureAdminQueryDto : TrailFeatureQueryDto
{
    public bool? IsAvailable { get; set; }
}
