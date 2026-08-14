using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class MountainEquipmentSuggestion
{
    public long SuggestionId { get; set; }

    public long MountainId { get; set; }

    public long EquipmentId { get; set; }

    public string Season { get; set; } = null!;

    public int MinimumDays { get; set; }

    public int? MaximumDays { get; set; }

    public string? IntensityLevel { get; set; }

    public string? ExperienceLevel { get; set; }

    public int SuggestedQuantity { get; set; }

    public string RequirementLevel { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual Mountain Mountain { get; set; } = null!;
}
