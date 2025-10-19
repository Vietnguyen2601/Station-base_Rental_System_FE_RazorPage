using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.PromotionDTOs
{
    public class CreatePromotionRequestDTO
    {
        [Required]
        public string PromoCode { get; set; }
        [Required]
        public decimal DiscountPercentage { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
    }
}
