namespace prjGoHike.ViewModels
{
    //建議清單中的「一件裝備」
    public class CEquipmentSuggestionItemViewModel
    {
        public long EquipmentId { get; set; }

        public string CategoryName { get; set; } = "";

        public string EquipmentName { get; set; } = "";

        public int Quantity { get; set; } = 1;

        public int UnitWeightGram { get; set; }

        public string RequirementLevel { get; set; } = "";

        public string? Notes { get; set; }

        public bool IsSelected { get; set; }

        public int TotalWeightGram
        {
            get
            {
                return Quantity * UnitWeightGram;
            }
        }
    }
}