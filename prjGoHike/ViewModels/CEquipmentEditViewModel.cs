using System.ComponentModel;

namespace prjGoHike.ViewModels
{
    public class CEquipmentEditViewModel
    {
        public CEquipmentConditionViewModel Condition
        {
            get;
            set;
        } = new CEquipmentConditionViewModel();

        public string MountainName { get; set; } = "";

        public List<CPersonalEquipmentItemViewModel> Items
        {
            get;
            set;
        } = new List<CPersonalEquipmentItemViewModel>();

        [DisplayName("自訂裝備名稱")]
        public string NewCustomEquipmentName
        {
            get;
            set;
        } = "";

        [DisplayName("數量")]
        public int NewCustomQuantity
        {
            get;
            set;
        } = 1;

        [DisplayName("單件重量")]
        public int NewCustomUnitWeightGram
        {
            get;
            set;
        }

        public int TotalWeightGram
        {
            get
            {
                return Items.Sum(
                    item => item.TotalWeightGram);
            }
        }

        public decimal TotalWeightKg
        {
            get
            {
                return TotalWeightGram / 1000M;
            }
        }
    }
}