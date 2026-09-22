using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Mountains
{
    public long MountainId { get; set; }

    public string MountainName { get; set; } = null!;

    public string Location { get; set; } = null!;

    public int Altitude { get; set; }

    public int DifficultyLevel { get; set; }

    public bool? MountainsPermitRequired { get; set; }

    public bool? NationalParkPermitRequired { get; set; }

    public decimal? Longitude { get; set; }

    public decimal? Latitude { get; set; }

    public virtual ICollection<EventData> EventData { get; set; } = new List<EventData>();

    public virtual ICollection<HikeRecords> HikeRecords { get; set; } = new List<HikeRecords>();

    public virtual ICollection<MountainEquipmentSuggestions> MountainEquipmentSuggestions { get; set; } = new List<MountainEquipmentSuggestions>();

    public virtual ICollection<PersonalEquipmentLists> PersonalEquipmentLists { get; set; } = new List<PersonalEquipmentLists>();
}
