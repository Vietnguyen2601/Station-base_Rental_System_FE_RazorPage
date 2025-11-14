using System;

namespace EVStationRental.Common.DTOs.Realtime;

public sealed class VehicleUpdatedPayload
{
    public Guid VehicleId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Status { get; set; }
    public bool? IsActive { get; set; }
    public Guid? StationId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
