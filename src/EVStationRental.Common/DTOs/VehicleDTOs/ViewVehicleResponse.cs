using System;

namespace EVStationRental.Common.DTOs.VehicleDTOs
{
    public class ViewVehicleResponse
    {
        public Guid VehicleId { get; set; }
        public string SerialNumber { get; set; }
        public string TypeName { get; set; } // VehicleType
        public string ModelName { get; set; }
        public string Manufacturer { get; set; }
        public decimal PricePerHour { get; set; }
        public int? BatteryLevel { get; set; }
        public int? BatteryCapacity { get; set; }
        public int? Range { get; set; }
        public string? Color { get; set; }
        public string? Img { get; set; }
        public string? StationName { get; set; }
        public string? Status { get; set; }
        public DateOnly? LastMaintenance { get; set; }
        public string? Specs { get; set; }
    }
}
