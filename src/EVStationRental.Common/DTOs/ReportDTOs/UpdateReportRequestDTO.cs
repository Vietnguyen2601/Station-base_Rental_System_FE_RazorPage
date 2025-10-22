using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.ReportDTOs
{
    public class UpdateReportRequestDTO
    {
        [Required(ErrorMessage = "Lo?i báo cáo là b?t bu?c")]
        public string ReportType { get; set; } = null!;

        [Required(ErrorMessage = "N?i dung báo cáo là b?t bu?c")]
        [StringLength(5000, ErrorMessage = "N?i dung báo cáo không ???c v??t quá 5000 ký t?")]
        public string Text { get; set; } = null!;
    }
}
