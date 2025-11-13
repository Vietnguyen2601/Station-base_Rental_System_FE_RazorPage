using System;
using System.Collections.Generic;

namespace EVStationRental.Presentation.Models.Admin.Dashboard;

public class StationVehicleUsageResponse
{
    public string? FilterType { get; set; }
    public string? FilterDescription { get; set; }
    public DateTime? Timestamp { get; set; }
    public List<StationVehicleUsageItem> Stations { get; set; } = new();
}

public class StationVehicleUsageItem
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int TotalVehicles { get; set; }
    public int AvailableVehicles { get; set; }
    public int RentedVehicles { get; set; }
    public int MaintenanceVehicles { get; set; }
    public int ChargingVehicles { get; set; }
    public decimal UsageRate { get; set; }
    public decimal AvailabilityRate { get; set; }
}
