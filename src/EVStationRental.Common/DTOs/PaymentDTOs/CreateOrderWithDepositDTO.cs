using System;

namespace EVStationRental.Common.DTOs.PaymentDTOs
{
    public class CreateOrderWithDepositDTO
    {
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public string ReturnUrl { get; set; } = "";
        public string? IpAddress { get; set; }
        public Guid? PromotionId { get; set; }
        public Guid? StaffId { get; set; }
    }
}