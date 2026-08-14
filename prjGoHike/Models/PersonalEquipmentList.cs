using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class PersonalEquipmentList
{
    public long ListId { get; set; }

    public long MemberId { get; set; }

    public long MountainId { get; set; }

    public string ListName { get; set; } = null!;

    public DateOnly HikingDate { get; set; }

    public int HikingDays { get; set; }

    public string Season { get; set; } = null!;

    public string? IntensityLevel { get; set; }

    public string? ExperienceLevel { get; set; }

    public decimal BodyWeightKg { get; set; }

    public int MaxCarryWeightGram { get; set; }

    public int TotalWeightGram { get; set; }

    public int RemainingWeightGram { get; set; }

    public decimal WeightPercentage { get; set; }

    public string WeightStatus { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User Member { get; set; } = null!;

    public virtual Mountain Mountain { get; set; } = null!;

    public virtual ICollection<PersonalEquipmentDetail> PersonalEquipmentDetails { get; set; } = new List<PersonalEquipmentDetail>();
}
