using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// DTO for finalizing payment when returning vehicle
    /// </summary>
    public class FinalizeReturnPaymentDTO
    {
        /// <summary>
        /// Amount to deduct from user's wallet
        /// </summary>
        [Required(ErrorMessage = "Số tiền là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment method for final payment: "WALLET", "CASH", "VNPAY", etc.
        /// </summary>
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public string FinalPaymentMethod { get; set; } = "WALLET";
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
        public decimal FinalAmountDue { get; set; }  // Amount customer needs to pay (or receive if negative = refund)
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public string OrderStatus { get; set; } = null!;
        public DateTime CompletedAt { get; set; }
        public string? PaymentUrl { get; set; }  // If using external gateway for final payment
        public string Message { get; set; } = null!;
    }
}
