namespace prjGoHike.DTO.GoHikeSafe;

public class TrailIndicatorsDto
{
    public long TrailId { get; set; }

    public bool HasIndicators => Indicators.Count > 0;

    public List<TrailRelatedIndicatorDto> Indicators { get; set; } = [];
}

public class TrailRelatedIndicatorDto
{
    public long IndicatorId { get; set; }

    public string IndicatorName { get; set; } = null!;

    public string IndicatorType { get; set; } = null!;

    public byte? IndicatorLevel { get; set; }

    public decimal? DistanceMeters { get; set; }

    public decimal? EvaluatedScore { get; set; }
}
