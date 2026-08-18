using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.ViewModels
{
    public class CPersonalEquipmentItemViewModel
    {
        public long? EquipmentId { get; set; }

        [DisplayName("裝備名稱")]
        public string EquipmentName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        [DisplayName("數量")]
        [Range(
            1,
            20,
            ErrorMessage = "數量必須介於 1 至 20")]
        public int Quantity { get; set; } = 1;

        [DisplayName("單件重量")]
        [Range(
            0,
            50000,
            ErrorMessage = "單件重量必須介於 0 至 50000 公克")]
        public int UnitWeightGram { get; set; }

        public string RequirementLevel { get; set; } = "";

        public string? Notes { get; set; }

        public bool IsCustomEquipment
        {
            get
            {
                return EquipmentId == null;
            }
        }

        public int TotalWeightGram
        {
            get
            {
                return Quantity * UnitWeightGram;
            }
        }
    }
}
