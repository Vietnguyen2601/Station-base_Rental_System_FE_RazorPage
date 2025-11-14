using System;

namespace EVStationRental.Common.DTOs.Realtime;

public sealed class StationUpdatedPayload
{
    public Guid StationId { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
