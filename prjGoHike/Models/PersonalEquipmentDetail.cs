using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class PersonalEquipmentDetail
{
    public long DetailId { get; set; }

    public long ListId { get; set; }

    public long? EquipmentId { get; set; }

    public string? CustomEquipmentName { get; set; }

    public int Quantity { get; set; }

    public int UnitWeightGram { get; set; }

    public int TotalWeightGram { get; set; }

    public string? RequirementLevel { get; set; }

    public bool IsPrepared { get; set; }

    public int SortOrder { get; set; }

    public string? Notes { get; set; }

    public virtual Equipment? Equipment { get; set; }

    public virtual PersonalEquipmentList List { get; set; } = null!;
}
