using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.OrderDTOs
{
    public class CreateOrderRequestDTO
    {
        [Required(ErrorMessage = "VehicleId là b?t bu?c")]
        public Guid VehicleId { get; set; }

        [Required(ErrorMessage = "Th?i gian b?t ??u là b?t bu?c")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Th?i gian k?t thúc d? ki?n là b?t bu?c")]
        public DateTime EndTime { get; set; }

        public string? PromotionCode { get; set; }
    }
}
