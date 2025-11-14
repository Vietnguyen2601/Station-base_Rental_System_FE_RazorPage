using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Presentation.Models.Orders;

public class OrderCreateSummaryVm
{
    public Guid VehicleId { get; set; }
    public Guid StationId { get; set; }

    public string VehicleName { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public decimal BasePrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAfterDiscount { get; set; }
    public decimal DepositAmount { get; set; }

    public string PaymentMethod { get; set; } = "WALLET";
    public string? PromotionCode { get; set; }

    [Display(Name = "Tôi hiểu đặt cọc 10% là bắt buộc")]
    public bool AcceptDepositPolicy { get; set; }

    [Display(Name = "Tôi cam kết tuân thủ quy định sử dụng xe & thời gian hoàn trả")]
    public bool AcceptUsagePolicy { get; set; }
}
