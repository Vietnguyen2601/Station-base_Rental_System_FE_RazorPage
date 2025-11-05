using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// DTO for finalizing payment when returning vehicle
    /// </summary>
    public class FinalizeReturnPaymentDTO
    {
        [Required(ErrorMessage = "OrderId là bắt buộc")]
        public Guid OrderId { get; set; }

        /// <summary>
        /// Payment method for final payment: "WALLET", "CASH", "VNPAY", etc.
        /// </summary>
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public string FinalPaymentMethod { get; set; } = "WALLET";

        /// <summary>
        /// Optional: Extra charges for damages, late return, etc.
        /// </summary>
        public decimal? ExtraCharges { get; set; }

        /// <summary>
        /// Description for extra charges
        /// </summary>
        public string? ExtraChargesDescription { get; set; }
    }

    /// <summary>
    /// Response after finalizing return payment
    /// </summary>
    public class FinalizeReturnPaymentResponseDTO
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public decimal TotalPrice { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal ExtraCharges { get; set; }
        public decimal FinalAmountDue { get; set; }  // Amount customer needs to pay (or receive if negative = refund)
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public string OrderStatus { get; set; } = null!;
        public DateTime CompletedAt { get; set; }
        public string? PaymentUrl { get; set; }  // If using external gateway for final payment
        public string Message { get; set; } = null!;
    }
}
