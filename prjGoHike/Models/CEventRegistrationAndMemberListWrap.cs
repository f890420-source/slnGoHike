using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.Models;

public class CEventRegistrationAndMemberListWarp
{
    private EventRegistrationAndMemberList _CEventRegistrationAndMemberList;

    public CEventRegistrationAndMemberListWarp()
    {
        _CEventRegistrationAndMemberList = new EventRegistrationAndMemberList();
    }

    public EventRegistrationAndMemberList CEventRegistrationAndMemberList
    {
        get { return _CEventRegistrationAndMemberList; }
        set { _CEventRegistrationAndMemberList = value; }
    }

    [Key]
    [DisplayName("報名ID")]
    public long SignUpId
    {
        get { return _CEventRegistrationAndMemberList.SignUpId; }
        set { _CEventRegistrationAndMemberList.SignUpId = value; }
    }

    [DisplayName("使用者ID")]
    public long UserId
    {
        get { return _CEventRegistrationAndMemberList.UserId; }
        set { _CEventRegistrationAndMemberList.UserId = value; }
    }

    [DisplayName("活動ID")]
    public long EventId
    {
        get { return _CEventRegistrationAndMemberList.EventId; }
        set { _CEventRegistrationAndMemberList.EventId = value; }
    }

    [DisplayName("註冊有無成功")]
    public int RegistrationStatus
    {
        get { return _CEventRegistrationAndMemberList.RegistrationStatus; }
        set { _CEventRegistrationAndMemberList.RegistrationStatus = value; }
    }

    [DisplayName("緊急聯絡人")]
    public string EmergencyContact
    {
        get { return _CEventRegistrationAndMemberList.EmergencyContact; }
        set { _CEventRegistrationAndMemberList.EmergencyContact = value; }
    }

    [DisplayName("活動報名日期")]
    public DateTime CreatedAt
    {
        get { return _CEventRegistrationAndMemberList.CreatedAt; }
        set { _CEventRegistrationAndMemberList.CreatedAt = value; }
    }
}
