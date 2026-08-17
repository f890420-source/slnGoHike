using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.ViewModels
{
    public class CEquipmentConditionViewModel
    {
        [DisplayName("清單名稱")]
        [Required(ErrorMessage = "請輸入清單名稱")]
        [StringLength(
            100,
            ErrorMessage = "清單名稱不可超過 100 個字")]
        public string ListName { get; set; } = "";

        [DisplayName("山岳／路線")]
        [Range(
            1,
            long.MaxValue,
            ErrorMessage = "請選擇山岳")]
        public long MountainId { get; set; }

        [DisplayName("登山日期")]
        [Required(ErrorMessage = "請選擇登山日期")]
        [DataType(DataType.Date)]
        public DateTime HikingDate { get; set; }
            = DateTime.Today;

        [DisplayName("登山天數")]
        [Range(
            1,
            30,
            ErrorMessage = "登山天數必須介於 1 至 30 天")]
        public int HikingDays { get; set; } = 1;

        [DisplayName("登山季節")]
        [Required(ErrorMessage = "請選擇登山季節")]
        public string Season { get; set; } = "";

        [DisplayName("行程強度")]
        [Required(ErrorMessage = "請選擇行程強度")]
        public string IntensityLevel { get; set; } = "";

        [DisplayName("個人體重")]
        [Range(
            20,
            300,
            ErrorMessage = "個人體重必須介於 20 至 300 公斤")]
        public decimal BodyWeightKg { get; set; }

        [DisplayName("負重經驗")]
        [Required(ErrorMessage = "請選擇負重經驗")]
        public string ExperienceLevel { get; set; } = "";

        [DisplayName("安全負重上限")]
        public decimal MaxCarryWeightKg
        {
            get
            {
                return Math.Round(
                    BodyWeightKg * 0.20M,
                    2);
            }
        }
    }
}