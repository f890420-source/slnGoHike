using System.ComponentModel.DataAnnotations;

namespace prjGoHike.DTO.PersonalEquipment;

public class CreatePersonalEquipmentListRequestDto
{
    [Required]
    [MaxLength(100)]
    public string ListName { get; set; } = "";

    [Range(1, long.MaxValue)]
    public long MountainId { get; set; }

    [Required]
    public DateTime HikingDate { get; set; }

    [Range(1, 30)]
    public int HikingDays { get; set; }

    [Required]
    public string Season { get; set; } = "";

    [Required]
    public string IntensityLevel { get; set; } = "";

    [Required]
    public string ExperienceLevel { get; set; } = "";

    [Range(20, 300)]
    public decimal BodyWeightKg { get; set; }

    [MinLength(1)]
    public List<CreatePersonalEquipmentItemDto> Items { get; set; } = [];
}

public class CreatePersonalEquipmentItemDto
{
    [Range(1, long.MaxValue)]
    public long SuggestionId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; }
}
