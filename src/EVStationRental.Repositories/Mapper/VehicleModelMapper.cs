using EVStationRental.Common.DTOs.VehicleModelDTOs;
using EVStationRental.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.Mapper
{
    public static class VehicleModelMapper
    {
        public static ViewVehicleModelResponseDTO ToViewVehicleModelDTO(this VehicleModel vehicleModel)
        {
            return new ViewVehicleModelResponseDTO
            {
                VehicleModelId = vehicleModel.VehicleModelId,
                TypeId = vehicleModel.TypeId,
                Name = vehicleModel.Name,
                Manufacturer = vehicleModel.Manufacturer,
                PricePerHour = vehicleModel.PricePerHour,
                Specs = vehicleModel.Specs,
                UpdatedAt = vehicleModel.UpdatedAt,
                CreatedAt = vehicleModel.CreatedAt,
                Isactive = vehicleModel.Isactive

            };
        }

        public static VehicleModel ToVehicleModel(this CreateVehicleModelRequestDTO dto)
        {
            return new VehicleModel
            {
                VehicleModelId = Guid.NewGuid(),
                TypeId = dto.TypeId,
                Name = dto.Name,
                Manufacturer = dto.Manufacturer,
                PricePerHour = dto.PricePerHour,
                Specs = dto.Specs,
                CreatedAt = DateTime.Now,
                Isactive = true
            };
        }

        public static void MapToVehicleModel(this UpdateVehicleModelRequestDTO dto, VehicleModel vehicleModel)
        {
            if (dto.TypeId != Guid.Empty) vehicleModel.TypeId = dto.TypeId;
            if (dto.Name != null) vehicleModel.Name = dto.Name;
            if (dto.Manufacturer != null) vehicleModel.Manufacturer = dto.Manufacturer;
            if (dto.PricePerHour != null) vehicleModel.PricePerHour = dto.PricePerHour;
            if (dto.Specs != null) vehicleModel.Specs = dto.Specs;
            vehicleModel.UpdatedAt = DateTime.Now;
        }
    }
}
