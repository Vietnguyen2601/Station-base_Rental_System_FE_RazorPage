using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Common.DTOs.VehicleModelDTOs
{
    public class UpdateVehicleModelRequestDTO
    {
        public Guid TypeId { get; set; }
        public string Name { get; set; } = null!;
        public string Manufacturer { get; set; } = null!;
        public decimal PricePerHour { get; set; }
        public string? Specs { get; set; }
    }
}
