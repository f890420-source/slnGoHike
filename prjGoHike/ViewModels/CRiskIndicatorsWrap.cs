using NetTopologySuite.Geometries;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using prjGoHike.Models;

namespace prjGoHike.ViewModels;

public class CRiskIndicatorsWrap
{
    private RiskIndicator _riskIndicator;

    [ScaffoldColumn(false)]
    public RiskIndicator riskIndicator
    {
        get { return _riskIndicator; }
        set { _riskIndicator = value; }
    }

    public CRiskIndicatorsWrap()
    {
        _riskIndicator = new RiskIndicator();
    }

    public CRiskIndicatorsWrap(RiskIndicator riskIndicator)
    {
        _riskIndicator = riskIndicator;
    }

    [Key]
    public long RiskIndicatorId
    {
        get { return _riskIndicator.RiskIndicatorId; }
        set { _riskIndicator.RiskIndicatorId = value; }
    }

    [DisplayName("指標名稱")]
    public string IndicatorName
    {
        get { return _riskIndicator.IndicatorName; }
        set { _riskIndicator.IndicatorName = value; }
    }

    [DisplayName("指標類型")]
    public string IndicatorType
    {
        get { return _riskIndicator.IndicatorType; }
        set { _riskIndicator.IndicatorType = value; }
    }

    [DisplayName("指標權重")]
    public decimal Weight
    {
        get { return _riskIndicator.Weight; }
        set { _riskIndicator.Weight = value; }
    }

    [DisplayName("指標風險等級")]
    public byte RiskLevel
    {
        get { return _riskIndicator.RiskLevel; }
        set { _riskIndicator.RiskLevel = value; }
    }

    [DisplayName("指標敘述")]
    public string? IndicatorDescription
    {
        get { return _riskIndicator.IndicatorDescription; }
        set { _riskIndicator.IndicatorDescription = value; }
    }

    [DisplayName("指標有效起始時間")]
    public DateTime? ValidFrom
    {
        get { return _riskIndicator.ValidFrom; }
        set { _riskIndicator.ValidFrom = value; }
    }

    [DisplayName("指標有效終止時間")]
    public DateTime? ValidTo
    {
        get { return _riskIndicator.ValidTo; }
        set { _riskIndicator.ValidTo = value; }
    }

    [DisplayName("指標資料來源")]
    public string? DataSource
    {
        get { return _riskIndicator.DataSource; }
        set { _riskIndicator.DataSource = value; }
    }

    [DisplayName("是否已啟用指標")]
    public bool IsActive
    {
        get { return _riskIndicator.IsActive; }
        set { _riskIndicator.IsActive = value; }
    }

    [ScaffoldColumn(false)]
    public Geometry? SpatialArea
    {
        get { return _riskIndicator.SpatialArea; }
        set { _riskIndicator.SpatialArea = value; }
    }
}
