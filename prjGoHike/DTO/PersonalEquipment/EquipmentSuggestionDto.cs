namespace prjGoHike.DTO.PersonalEquipment;

public class EquipmentSuggestionDto
{
    public long SuggestionId { get; set; }

    public long EquipmentId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int CategorySortOrder { get; set; }

    public string EquipmentName { get; set; } = string.Empty;

    public int StandardWeightGram { get; set; }

    public int SuggestedQuantity { get; set; }

    public int TotalWeightGram { get; set; }

    public string RequirementLevel { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? Notes { get; set; }
}
