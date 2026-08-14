using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.Models;

public partial class Mountain
{
    
    public long MountainId { get; set; }

    public string MountainName { get; set; } = null!;

    public string Location { get; set; } = null!;

    public int Altitude { get; set; }

    public int DifficultyLevel { get; set; }

    public int? MountainsPermitRequired { get; set; }

    public int? NationalParkPermitRequired { get; set; }

    public virtual ICollection<EventDatum> EventData { get; set; } = new List<EventDatum>();

    public virtual ICollection<HikeRecord> HikeRecords { get; set; } = new List<HikeRecord>();

    public virtual ICollection<MountainEquipmentSuggestion> MountainEquipmentSuggestions { get; set; } = new List<MountainEquipmentSuggestion>();

    public virtual ICollection<PersonalEquipmentList> PersonalEquipmentLists { get; set; } = new List<PersonalEquipmentList>();
}
