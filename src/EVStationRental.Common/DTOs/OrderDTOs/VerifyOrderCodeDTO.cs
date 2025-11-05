using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.OrderDTOs
{
    /// <summary>
    /// DTO for staff to verify order code when customer picks up vehicle
    /// </summary>
    public class VerifyOrderCodeDTO
    {
        [Required(ErrorMessage = "Mã đơn hàng là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã đơn hàng phải có 6 ký tự")]
        public string OrderCode { get; set; } = null!;
    }

    /// <summary>
    /// Response after verifying order code
    /// </summary>
    public class VerifyOrderCodeResponseDTO
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;
        public Guid VehicleId { get; set; }
        public string VehicleLicensePlate { get; set; } = null!;
        public string VehicleModel { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositPaid { get; set; }
        public string OrderStatus { get; set; } = null!;
        public bool IsDepositPaid { get; set; }
        public Guid ContractId { get; set; }
        public string ContractFileUrl { get; set; } = null!;
    }
}
