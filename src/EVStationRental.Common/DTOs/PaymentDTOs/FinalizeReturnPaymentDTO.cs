using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.PaymentDTOs
{
    public class FinalizeReturnPaymentDTO
    {
        [Required(ErrorMessage = "AccountId là bắt buộc")]
        public Guid AccountId { get; set; }

        [Required(ErrorMessage = "Số tiền là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public string FinalPaymentMethod { get; set; } = "WALLET";
    }

    public class FinalizeReturnPaymentResponseDTO
    {
        public Guid AccountId { get; set; }
        public decimal AmountDeducted { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public DateTime CompletedAt { get; set; }
        public string Message { get; set; } = null!;
    }
}
