using System;

namespace EVStationRental.Common.DTOs.PaymentDTOs
{
    public class PriceCalculationResponseDTO
    {
        public Guid OrderId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal DepositPrice { get; set; }
        public decimal? PromotionPrice { get; set; }
        public decimal? PromotionPercentage { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
