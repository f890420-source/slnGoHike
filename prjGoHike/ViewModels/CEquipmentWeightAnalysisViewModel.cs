namespace prjGoHike.ViewModels
{
    public class CEquipmentCategoryWeightViewModel
    {
        public string CategoryName { get; set; } = "";

        public int ItemCount { get; set; }

        public int TotalWeightGram { get; set; }

        public decimal TotalWeightKg
        {
            get
            {
                return TotalWeightGram / 1000M;
            }
        }

        public decimal WeightPercentage { get; set; }
    }

    public class CEquipmentWeightAnalysisViewModel
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

        public decimal MaxCarryWeightKg
        {
            get
            {
                return Condition.MaxCarryWeightKg;
            }
        }

        public decimal BodyWeightPercentage
        {
            get
            {
                if (Condition.BodyWeightKg <= 0)
                {
                    return 0;
                }

                return Math.Round(
                    TotalWeightKg /
                    Condition.BodyWeightKg * 100M,
                    2);
            }
        }

        public decimal LoadLimitUsagePercentage
        {
            get
            {
                if (MaxCarryWeightKg <= 0)
                {
                    return 0;
                }

                return Math.Round(
                    TotalWeightKg /
                    MaxCarryWeightKg * 100M,
                    2);
            }
        }

        public decimal ProgressPercentage
        {
            get
            {
                return Math.Min(
                    LoadLimitUsagePercentage,
                    100M);
            }
        }

        public bool IsOverweight
        {
            get
            {
                return TotalWeightKg >
                    MaxCarryWeightKg;
            }
        }

        public decimal RemainingWeightKg
        {
            get
            {
                return Math.Max(
                    MaxCarryWeightKg -
                    TotalWeightKg,
                    0M);
            }
        }

        public decimal OverweightKg
        {
            get
            {
                return Math.Max(
                    TotalWeightKg -
                    MaxCarryWeightKg,
                    0M);
            }
        }

        public string WeightStatus
        {
            get
            {
                if (IsOverweight)
                {
                    return "超過安全上限";
                }

                if (LoadLimitUsagePercentage >= 80M)
                {
                    return "接近安全上限";
                }

                return "安全範圍";
            }
        }

        public string StatusBootstrapClass
        {
            get
            {
                if (IsOverweight)
                {
                    return "danger";
                }

                if (LoadLimitUsagePercentage >= 80M)
                {
                    return "warning";
                }

                return "success";
            }
        }

        public List<CEquipmentCategoryWeightViewModel>
            CategoryWeights
        {
            get
            {
                return Items
                    .GroupBy(item =>
                        string.IsNullOrWhiteSpace(
                            item.CategoryName)
                            ? "其他"
                            : item.CategoryName)
                    .Select(group =>
                    {
                        int categoryWeightGram =
                            group.Sum(item =>
                                item.TotalWeightGram);

                        decimal percentage =
                            TotalWeightGram == 0
                                ? 0
                                : Math.Round(
                                    categoryWeightGram /
                                    (decimal)TotalWeightGram *
                                    100M,
                                    2);

                        return new
                            CEquipmentCategoryWeightViewModel
                        {
                            CategoryName = group.Key,
                            ItemCount = group.Count(),
                            TotalWeightGram =
                                categoryWeightGram,
                            WeightPercentage =
                                percentage
                        };
                    })
                    .OrderByDescending(category =>
                        category.TotalWeightGram)
                    .ToList();
            }
        }
    }
}
