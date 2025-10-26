using System;

namespace EVStationRental.Common.DTOs.StationDTOs
{
    public class StationWithAvailableVehiclesResponse
    {
        public Guid StationId { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int Capacity { get; set; }
        public decimal Lat { get; set; }
        public decimal Long { get; set; }
        public int AvailableVehicleCount { get; set; }
    }
}
