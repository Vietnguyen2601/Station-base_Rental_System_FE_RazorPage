using System;

namespace EVStationRental.Common.DTOs.OrderDTOs;

public sealed class OrderPriceEstimateDTO
{
    public decimal BasePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public Guid? PromotionId { get; set; }
}
