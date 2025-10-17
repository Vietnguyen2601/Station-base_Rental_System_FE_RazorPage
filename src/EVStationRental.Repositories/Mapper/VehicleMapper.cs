using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Repositories.Models;
using System;
using EVStationRental.Common.Enums.EnumModel;

namespace EVStationRental.Repositories.Mapper
{
    public static class VehicleMapper
    {
        public static ViewVehicleResponse ToViewVehicleDTO(this Vehicle vehicle)
        {
            return new ViewVehicleResponse
            {
                VehicleId = vehicle.VehicleId,
                SerialNumber = vehicle.SerialNumber,
                TypeName = vehicle.Model?.Type?.TypeName ?? string.Empty,
                ModelName = vehicle.Model?.Name ?? string.Empty,
                Manufacturer = vehicle.Model?.Manufacturer ?? string.Empty,
                PricePerHour = vehicle.Model?.PricePerHour ?? 0,
                BatteryLevel = vehicle.BatteryLevel,
                BatteryCapacity = vehicle.BatteryCapacity,
                Range = vehicle.Range,
                Color = vehicle.Color,
                Img = vehicle.Img,
                StationName = vehicle.Station?.Name ?? string.Empty,
                Status = vehicle.Status.ToString(),
                LastMaintenance = vehicle.LastMaintenance,
                Specs = vehicle.Model?.Specs
            };
        }

        public static Vehicle ToVehicle(this CreateVehicleRequestDTO dto)
        {
            return new Vehicle
            {
                VehicleId = Guid.NewGuid(),
                SerialNumber = dto.SerialNumber,
                ModelId = dto.ModelId,
                StationId = dto.StationId,
                BatteryLevel = dto.BatteryLevel,
                BatteryCapacity = dto.BatteryCapacity,
                Range = dto.Range,
                Color = dto.Color,
                LastMaintenance = dto.LastMaintenance,
                Img = dto.Img,
                CreatedAt = DateTime.Now,
                Isactive = dto.Isactive ?? true
            };
        }

        public static void MapToVehicle(this UpdateVehicleRequestDTO dto, Vehicle vehicle)
        {
            if (dto.StationId != null) vehicle.StationId = dto.StationId;
            if (dto.ModelId != null) vehicle.ModelId = dto.ModelId.Value;
            if (dto.BatteryLevel != null) vehicle.BatteryLevel = dto.BatteryLevel;
            if (dto.BatteryCapacity != null) vehicle.BatteryCapacity = dto.BatteryCapacity;
            if (dto.Range != null) vehicle.Range = dto.Range;
            if (dto.Color != null) vehicle.Color = dto.Color;
            if (dto.LastMaintenance != null) vehicle.LastMaintenance = dto.LastMaintenance;
            if (dto.Img != null) vehicle.Img = dto.Img;
            if (dto.Isactive != null) vehicle.Isactive = dto.Isactive.Value;
            vehicle.UpdatedAt = DateTime.Now;
        }

        private static string GetTypeNameFromDb(Guid modelId)
        {
            // Truy vấn DB để lấy TypeName nếu cần
            return string.Empty;
        }
        private static string GetModelNameFromDb(Guid modelId)
        {
            return string.Empty;
        }
        private static string GetManufacturerFromDb(Guid modelId)
        {
            return string.Empty;
        }
        private static decimal GetPricePerHourFromDb(Guid modelId)
        {
            return 0;
        }
        private static string GetStationNameFromDb(Guid? stationId)
        {
            return string.Empty;
        }
    }
}
