using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Equipment
{
    public long EquipmentId { get; set; }

    public long CategoryId { get; set; }

    public string EquipmentName { get; set; } = null!;

    public int StandardWeightGram { get; set; }

    public string RequirementLevel { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual EquipmentCategory Category { get; set; } = null!;

    public virtual ICollection<MountainEquipmentSuggestion> MountainEquipmentSuggestions { get; set; } = new List<MountainEquipmentSuggestion>();

    public virtual ICollection<PersonalEquipmentDetail> PersonalEquipmentDetails { get; set; } = new List<PersonalEquipmentDetail>();
}
