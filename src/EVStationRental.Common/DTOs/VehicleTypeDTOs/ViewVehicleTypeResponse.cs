using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Common.DTOs.VehicleTypeDTOs
{
    public class ViewVehicleTypeResponse
    {
        public Guid VehicleTypeId { get; set; }

        public string TypeName { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool Isactive { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
