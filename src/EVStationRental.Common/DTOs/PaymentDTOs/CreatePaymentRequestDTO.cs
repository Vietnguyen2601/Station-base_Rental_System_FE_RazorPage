using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Common.DTOs.PaymentDTOs
{
    public class CreatePaymentRequestDTO
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public string? ReturnUrl { get; set; }

        public string? CancelUrl { get; set; }
    }
}