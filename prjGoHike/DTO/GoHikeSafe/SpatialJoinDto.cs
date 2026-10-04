using System.ComponentModel.DataAnnotations;
using prjGoHike.Services.SpatialJoins;

namespace prjGoHike.DTO.GoHikeSafe;

public sealed class SpatialJoinRequestDto : IValidatableObject
{
    [Required]
    public decimal? DistanceMeters { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DistanceMeters is decimal distance && !SpatialJoinRules.IsValidDistance(distance))
            yield return new ValidationResult("距離必須介於 0.01～10000.00 公尺，且最多兩位小數。", [nameof(DistanceMeters)]);
    }
}

public sealed class SpatialJoinQueryDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class SpatialJoinAssociationQueryDto : IValidatableObject
{
    [Range(1, long.MaxValue)]
    public long? AfterTrailId { get; set; }

    [Range(1, long.MaxValue)]
    public long? AfterIndicatorId { get; set; }

    [Range(1, long.MaxValue)]
    public long? TrailId { get; set; }

    [Range(1, long.MaxValue)]
    public long? IndicatorId { get; set; }

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AfterTrailId.HasValue != AfterIndicatorId.HasValue)
            yield return new ValidationResult("分頁游標必須同時提供 afterTrailId 與 afterIndicatorId。", [nameof(AfterTrailId), nameof(AfterIndicatorId)]);
    }
}

public sealed record SpatialJoinRunDto(long Id, string Status, decimal DistanceMeters,
    DateTime CreatedAt, DateTime? StartedAt, DateTime? FinishedAt, string? ErrorMessage, string StatusUrl);

// These are current unscored candidates, not historical results belonging to a Run.
public sealed record SpatialJoinAssociationDto(long TrailId, string TrailName, long IndicatorId,
    string IndicatorName, decimal? DistanceMeters, decimal IndicatorWeightSnapshot, DateTime EvaluatedAt);

public sealed record SpatialJoinPageDto<T>(IReadOnlyList<T> Items, bool HasMore);
