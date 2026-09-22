using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Equipments
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

    public virtual EquipmentCategories Category { get; set; } = null!;

    public virtual ICollection<MountainEquipmentSuggestions> MountainEquipmentSuggestions { get; set; } = new List<MountainEquipmentSuggestions>();

    public virtual ICollection<PersonalEquipmentDetails> PersonalEquipmentDetails { get; set; } = new List<PersonalEquipmentDetails>();
}
