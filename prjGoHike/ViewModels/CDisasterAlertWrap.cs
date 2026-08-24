using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using prjGoHike.Models;

namespace prjGoHike.ViewModels;

public class CDisasterAlertWrap
{
    private DisasterAlert _disasterAlert;

    [ScaffoldColumn(false)]
    public DisasterAlert disasterAlert
    {
        get { return _disasterAlert; }
        set { _disasterAlert = value; }
    }

    public CDisasterAlertWrap()
    {
        _disasterAlert = new DisasterAlert();
    }

    public CDisasterAlertWrap(DisasterAlert disasterAlert)
    {
        _disasterAlert = disasterAlert;
    }

    [Key]
    public long AlertId
    {
        get { return _disasterAlert.AlertId; }
        set { _disasterAlert.AlertId = value; }
    }

    [DisplayName("警示類型")]
    public string AlertType
    {
        get { return _disasterAlert.AlertType; }
        set { _disasterAlert.AlertType = value; }
    }

    [DisplayName("警示名稱")]
    public string AlertTitle
    {
        get { return _disasterAlert.AlertTitle; }
        set { _disasterAlert.AlertTitle = value; }
    }

    [DisplayName("警示敘述")]
    public string? AlertDescription
    {
        get { return _disasterAlert.AlertDescription; }
        set { _disasterAlert.AlertDescription = value; }
    }

    [DisplayName("警示嚴重等級")]
    public byte SeverityLevel
    {
        get { return _disasterAlert.SeverityLevel; }
        set { _disasterAlert.SeverityLevel = value; }
    }

    [DisplayName("警示有效起始日")]
    public DateTime EffectiveFrom
    {
        get { return _disasterAlert.EffectiveFrom; }
        set { _disasterAlert.EffectiveFrom = value; }
    }

    [DisplayName("警示有效結束日")]
    public DateTime? EffectiveTo
    {
        get { return _disasterAlert.EffectiveTo; }
        set { _disasterAlert.EffectiveTo = value; }
    }

    [DisplayName("發布來源機關")]
    public string? SourceAgency
    {
        get { return _disasterAlert.SourceAgency; }
        set { _disasterAlert.SourceAgency = value; }
    }

    [DisplayName("發布來源網址")]
    public string? SourceUrl
    {
        get { return _disasterAlert.SourceUrl; }
        set { _disasterAlert.SourceUrl = value; }
    }

    [DisplayName("是否已啟用警示")]
    public bool IsActive
    {
        get { return _disasterAlert.IsActive; }
        set { _disasterAlert.IsActive = value; }
    }
}
