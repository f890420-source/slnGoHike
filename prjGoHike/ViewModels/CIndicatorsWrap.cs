using NetTopologySuite.Geometries;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using prjGoHike.Models;

namespace prjGoHike.ViewModels;

public class CIndicatorsWrap
{
    private Indicator _indicator;

    [ScaffoldColumn(false)]
    public Indicator indicator
    {
        get { return _indicator; }
        set { _indicator = value; }
    }

    public CIndicatorsWrap()
    {
        _indicator = new Indicator();
    }

    public CIndicatorsWrap(Indicator indicator)
    {
        _indicator = indicator;
    }

    [Key]
    public long IndicatorId
    {
        get { return _indicator.IndicatorId; }
        set { _indicator.IndicatorId = value; }
    }

    [DisplayName("指標名稱")]
    public string IndicatorName
    {
        get { return _indicator.IndicatorName; }
        set { _indicator.IndicatorName = value; }
    }

    [DisplayName("指標類型")]
    public string IndicatorType
    {
        get { return _indicator.IndicatorType; }
        set { _indicator.IndicatorType = value; }
    }

    [DisplayName("指標權重")]
    public decimal Weight
    {
        get { return _indicator.Weight; }
        set { _indicator.Weight = value; }
    }

    [DisplayName("指標風險等級")]
    public byte? IndicatorLevel
    {
        get { return _indicator.IndicatorLevel; }
        set { _indicator.IndicatorLevel = value; }
    }

    [DisplayName("指標敘述")]
    public string? IndicatorDescription
    {
        get { return _indicator.IndicatorDescription; }
        set { _indicator.IndicatorDescription = value; }
    }

    [DisplayName("指標資料來源")]
    public string? DataSource
    {
        get { return _indicator.DataSource; }
        set { _indicator.DataSource = value; }
    }

    [DisplayName("是否已啟用指標")]
    public bool IsActive
    {
        get { return _indicator.IsActive; }
        set { _indicator.IsActive = value; }
    }
}
