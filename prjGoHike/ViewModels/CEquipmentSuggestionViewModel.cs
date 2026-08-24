namespace prjGoHike.ViewModels
{
    //類別代表整個裝備建議頁
    public class CEquipmentSuggestionViewModel
    {
        public CEquipmentConditionViewModel Condition
        {
            get;
            set;
        } = new CEquipmentConditionViewModel();

        public string MountainName { get; set; } = "";

        public List<CEquipmentSuggestionItemViewModel> Items
        {
            get;
            set;
        } = new List<CEquipmentSuggestionItemViewModel>();

        public int SelectedTotalWeightGram
        {
            get
            {
                return Items
                    .Where(item => item.IsSelected)
                    .Sum(item => item.TotalWeightGram);
            }
        }

        public decimal SelectedTotalWeightKg
        {
            get
            {
                return SelectedTotalWeightGram / 1000M;
            }
        }
    }
}