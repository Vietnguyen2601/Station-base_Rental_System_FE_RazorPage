using System;
using EVStationRental.Common.Enums.EnumModel;

namespace EVStationRental.Common.DTOs.OrderDTOs
{
    public class CreateOrderResponseDTO
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? PromotionCode { get; set; }
        public OrderStatus Status { get; set; }
        public string VehicleModelName { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
    }
}
