using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Common.DTOs.VehicleModelDTOs
{
    public class ViewVehicleModelResponseDTO
    {
        public Guid VehicleModelId { get; set; }
        public Guid TypeId { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public decimal PricePerHour { get; set; }
        public string? Specs { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Isactive { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
