using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Common.DTOs.VehicleTypeDTOs
{
    public class UpdateVehicleTypeRequestDTO
    {
        public string TypeName { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool Isactive { get; set; }
    }
}
