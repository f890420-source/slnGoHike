namespace prjGoHike.ViewModels
{
    public class CPersonalEquipmentListItemViewModel
    {
        public long ListId { get; set; }

        public string ListName { get; set; } = "";

        public string MountainName { get; set; } = "";

        public DateOnly HikingDate { get; set; }

        public int HikingDays { get; set; }

        public int TotalWeightGram { get; set; }

        public string WeightStatus { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public decimal TotalWeightKg
        {
            get
            {
                return TotalWeightGram / 1000M;
            }
        }

        public string StatusBootstrapClass
        {
            get
            {
                if (WeightStatus ==
                    "超過安全上限")
                {
                    return "danger";
                }

                if (WeightStatus ==
                    "接近安全上限")
                {
                    return "warning";
                }

                return "success";
            }
        }
    }

    public class CPersonalEquipmentListIndexViewModel
    {
        public string SearchKeyword { get; set; } = "";

        public string StatusFilter { get; set; } = "";

        public List<CPersonalEquipmentListItemViewModel>
            Items
        { get; set; }
            = new List<CPersonalEquipmentListItemViewModel>();
    }
}
