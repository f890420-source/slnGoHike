namespace prjGoHike.ViewModels
{
    public class CPersonalEquipmentListDetailItemViewModel
    {
        public string EquipmentName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public int Quantity { get; set; }

        public int UnitWeightGram { get; set; }

        public int TotalWeightGram { get; set; }

        public string RequirementLevel { get; set; } = "";

        public string? Notes { get; set; }

        public bool IsCustomEquipment { get; set; }

        public decimal UnitWeightKg
        {
            get
            {
                return UnitWeightGram / 1000M;
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

    public class CPersonalEquipmentListDetailsViewModel
    {
        public long ListId { get; set; }

        public string ListName { get; set; } = "";

        public string MountainName { get; set; } = "";

        public DateOnly HikingDate { get; set; }

        public int HikingDays { get; set; }

        public string Season { get; set; } = "";

        public string IntensityLevel { get; set; } = "";

        public string ExperienceLevel { get; set; } = "";

        public decimal BodyWeightKg { get; set; }

        public int MaxCarryWeightGram { get; set; }

        public int TotalWeightGram { get; set; }

        public int RemainingWeightGram { get; set; }

        public decimal WeightPercentage { get; set; }

        public string WeightStatus { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public List<CPersonalEquipmentListDetailItemViewModel>
            Items
        { get; set; }
            = new List<CPersonalEquipmentListDetailItemViewModel>();

        public decimal MaxCarryWeightKg
        {
            get
            {
                return MaxCarryWeightGram / 1000M;
            }
        }

        public decimal TotalWeightKg
        {
            get
            {
                return TotalWeightGram / 1000M;
            }
        }

        public decimal RemainingWeightKg
        {
            get
            {
                return RemainingWeightGram / 1000M;
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
}
