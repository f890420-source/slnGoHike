using NetTopologySuite.Geometries;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.Models;

public partial class CTrailWrap
{
    private Trail _trail;

    [ScaffoldColumn(false)]
    public Trail trail
    {
        get { return _trail; }
        set { _trail = value; }
    }

    public CTrailWrap()
    {
        _trail = new Trail();
    }

    public CTrailWrap(Trail trail)
    {
        _trail = trail;
    }

    [Key]
    public long TrailId
    {
        get { return _trail.TrailId; }
        set { _trail.TrailId = value; }
    }

    [DisplayName("步道名稱")]
    public string TrailName
    {
        get { return _trail.TrailName; }
        set { _trail.TrailName = value; }
    }

    [DisplayName("所在區域")]
    public string Region
    {
        get { return _trail.Region; }
        set { _trail.Region = value; }
    }

    [DisplayName("路線難度")]
    public int DifficultyLevel
    {
        get { return _trail.DifficultyLevel; }
        set { _trail.DifficultyLevel = value; }
    }

    [DisplayName("路線距離(公里)")]
    public decimal? DistanceKm
    {
        get { return _trail.DistanceKm; }
        set { _trail.DistanceKm = value; }
    }

    [ScaffoldColumn(false)]
    [DisplayName("預估步行時間")]
    public decimal? EstimatedHours
    {
        get { return _trail.EstimatedHours; }
        set { _trail.EstimatedHours = value; }
    }

    [ScaffoldColumn(false)]
    [DisplayName("是否需要申請許可")]
    public bool PermitRequired
    {
        get { return _trail.PermitRequired; }
        set { _trail.PermitRequired = value; }
    }

    [ScaffoldColumn(false)]
    [DisplayName("是否需要嚮導")]
    public bool GuideRequired
    {
        get { return _trail.GuideRequired; }
        set { _trail.GuideRequired = value; }
    }

    [ScaffoldColumn(false)]
    [DisplayName("其他規定")]
    public string? RegulationNote
    {
        get { return _trail.RegulationNote; }
        set { _trail.RegulationNote = value; }
    }

    [ScaffoldColumn(false)]
    public Geometry? TrailPath
    {
        get { return _trail.TrailPath; }
        set { _trail.TrailPath = value; }
    }

    [ScaffoldColumn(false)]
    [DisplayName("是否發布")]
    public bool IsPublished
    {
        get { return _trail.IsPublished; }
        set { _trail.IsPublished = value; }
    }
}
