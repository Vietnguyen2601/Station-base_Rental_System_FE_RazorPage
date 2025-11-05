using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.WalletDTOs
{
    /// <summary>
    /// DTO for topping up wallet
    /// </summary>
    public class TopUpWalletDTO
    {
        [Required(ErrorMessage = "Số tiền nạp là bắt buộc")]
        [Range(10000, 50000000, ErrorMessage = "Số tiền nạp phải từ 10,000 đến 50,000,000 VNĐ")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment method: VNPAY, BANK_TRANSFER, CASH
        /// </summary>
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public string PaymentMethod { get; set; } = null!;

        public string? Description { get; set; }
    }

    /// <summary>
    /// Response for wallet balance
    /// </summary>
    public class WalletBalanceDTO
    {
        public Guid WalletId { get; set; }
        public Guid AccountId { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Response for wallet transaction history
    /// </summary>
    public class WalletTransactionDTO
    {
        public Guid TransactionId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Response after top-up request
    /// </summary>
    public class TopUpResponseDTO
    {
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? PaymentUrl { get; set; }  // For VNPay redirect
        public string Message { get; set; } = null!;
    }
}
