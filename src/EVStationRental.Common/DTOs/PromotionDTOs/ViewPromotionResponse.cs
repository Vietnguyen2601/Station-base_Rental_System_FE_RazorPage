using System;

namespace EVStationRental.Common.DTOs.PromotionDTOs
{
    public class ViewPromotionResponse
    {
        public string PromoCode { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
