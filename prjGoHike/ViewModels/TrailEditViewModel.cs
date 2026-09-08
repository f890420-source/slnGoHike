using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace prjGoHike.ViewModels
{
    public class TrailEditViewModel
    {
        public long TrailId { get; set; }

        [Required(ErrorMessage = "請輸入步道名稱")]
        [StringLength(120)]
        [DisplayName("步道名稱")]
        public string TrailName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入所在區域")]
        [StringLength(80)]
        [DisplayName("所在區域")]
        public string Region { get; set; } = string.Empty;

        [Range(1, 5)]
        [DisplayName("路線難度")]
        public int DifficultyLevel { get; set; }

        [Range(0, 99999)]
        [DisplayName("路線距離（公里）")]
        public decimal? DistanceKm { get; set; }

        [DisplayName("是否需要申請許可")]
        public bool PermitRequired { get; set; }

        [DisplayName("是否需要嚮導")]
        public bool GuideRequired { get; set; }
        
        [DisplayName("其他規定")]
        public string? RegulationNote { get; set; }

        [DisplayName("重新上傳步道路線 GeoJSON")]
        public IFormFile? GeoJsonFile { get; set; }

        // 給 Edit 頁面顯示目前路線用，不需要送回 Controller
        public double[][] CurrentRouteCoordinates { get; set; }
        = Array.Empty<double[]>();

        public bool IsPublished { get; set; }
    }
}
