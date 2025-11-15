using System;

namespace EVStationRental.Common.DTOs.Realtime;

public sealed class OrderSummaryPayload
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
}
