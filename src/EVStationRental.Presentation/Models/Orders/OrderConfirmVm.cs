using System;

namespace EVStationRental.Presentation.Models.Orders;

public class OrderConfirmVm
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;

    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string PaymentMethodLabel { get; set; } = "Đặt cọc 10%";

    public decimal BasePrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAfterDiscount { get; set; }
    public decimal DepositAmount { get; set; }
}
