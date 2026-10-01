using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace prjGoHike.DTO.GoHikeSafe;

public class TrailAdminDto
{
    public long id { get; set; }

    [Required, StringLength(120)]
    public string TrailName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Region { get; set; } = string.Empty;

    [Range(1, 5)]
    public int DifficultyLevel { get; set; }

    [Range(typeof(decimal), "0", "99999")]
    public decimal? DistanceKm { get; set; }

    [Range(typeof(decimal), "0", "9999.99")]
    public decimal? EstimatedHours { get; set; }

    public bool PermitRequired { get; set; }
    public bool GuideRequired { get; set; }

    [StringLength(1000)]
    public string? RegulationNote { get; set; }

    public bool IsPublished { get; set; }
    public List<TrailAdminSegmentDto>? TrailSegDtos { get; set; }
}

public class TrailAdminSegmentDto
{
    public long id { get; set; }

    [Required]
    public Geometry Shape { get; set; } = null!;
}
