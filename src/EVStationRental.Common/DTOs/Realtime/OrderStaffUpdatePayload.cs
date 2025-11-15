using System;

namespace EVStationRental.Common.DTOs.Realtime;

public sealed class OrderStaffUpdatePayload
{
    public Guid OrderId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public Guid? StaffId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
