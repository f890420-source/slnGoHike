public class TrailMapViewModel
{
    public long TrailId { get; set; }

    public string TrailName { get; set; } = string.Empty;

    public List<TrailSegmentMapViewModel> Segments { get; set; } = [];
}

public class TrailSegmentMapViewModel
{
    public int TrailSegmentId { get; set; }

    public string? Source { get; set; }

    public double[][] Coordinates { get; set; } = [];
}