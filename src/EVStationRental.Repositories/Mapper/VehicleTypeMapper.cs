using EVStationRental.Common.DTOs.VehicleTypeDTOs;
using EVStationRental.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.Mapper
{
    public static class VehicleTypeMapper
    {
        public static ViewVehicleTypeResponse ToViewVehicleTypeDTO(this VehicleType vehicleType)
        {
            return new ViewVehicleTypeResponse
            {
                VehicleTypeId = vehicleType.VehicleTypeId,
                TypeName = vehicleType.TypeName,
                Description = vehicleType.Description,
                CreatedAt = vehicleType.CreatedAt,
                UpdatedAt = vehicleType.UpdatedAt,
                Isactive = vehicleType.Isactive

            };
        }

        public static VehicleType ToVehicleType(this CreateVehicleTypeRequestDTO dto)
        {
            return new VehicleType
            {
                VehicleTypeId = Guid.NewGuid(),
                TypeName = dto.TypeName,
                Description = dto.Description,
                CreatedAt = DateTime.Now,
                Isactive = true
            };
        }

        public static void MapToVehicleType(this UpdateVehicleTypeRequestDTO dto, VehicleType vehicleType)
        {
            if (dto.TypeName != null) vehicleType.TypeName = dto.TypeName;
            if (dto.Description != null) vehicleType.Description = dto.Description;
            if (dto.Isactive != null) vehicleType.Isactive = dto.Isactive;
            vehicleType.UpdatedAt = DateTime.Now;
        }
    }
}
