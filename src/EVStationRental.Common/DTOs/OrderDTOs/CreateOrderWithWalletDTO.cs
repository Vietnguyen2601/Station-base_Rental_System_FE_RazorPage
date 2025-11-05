using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.OrderDTOs
{
    /// <summary>
    /// DTO for creating order with wallet deposit
    /// </summary>
    public class CreateOrderWithWalletDTO
    {
        [Required(ErrorMessage = "VehicleId là bắt buộc")]
        public Guid VehicleId { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Payment method: "WALLET" or gateway name (e.g., "VNPAY")
        /// </summary>
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public string PaymentMethod { get; set; } = null!;

        /// <summary>
        /// Optional promotion code
        /// </summary>
        public string? PromotionCode { get; set; }
    }

    /// <summary>
    /// Response after creating order with deposit
    /// </summary>
    public class CreateOrderWithWalletResponseDTO
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public string Status { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string? PaymentUrl { get; set; }  // If using external gateway
        public VehicleInfoDTO Vehicle { get; set; } = null!;
        public ContractInfoDTO Contract { get; set; } = null!;
    }

    public class VehicleInfoDTO
    {
        public Guid VehicleId { get; set; }
        public string LicensePlate { get; set; } = null!;
        public string ModelName { get; set; } = null!;
    }

    public class ContractInfoDTO
    {
        public Guid ContractId { get; set; }
        public DateTime ContractDate { get; set; }
        public string FileUrl { get; set; } = null!;
    }
}
