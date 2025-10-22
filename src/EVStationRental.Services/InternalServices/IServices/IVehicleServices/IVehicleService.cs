using System.Threading.Tasks;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Services.Base;
using System;
using System.Collections.Generic;

namespace EVStationRental.Services.InternalServices.IServices.IVehicleServices
{
    public interface IVehicleService
    {
        Task<IServiceResult> GetAllVehiclesAsync();
        Task<IServiceResult> GetVehicleByIdAsync(Guid id);
        Task<IServiceResult> CreateVehicleAsync(CreateVehicleRequestDTO dto);
        Task<IServiceResult> UpdateVehicleAsync(Guid vehicleId, UpdateVehicleRequestDTO dto);
        Task<IServiceResult> SoftDeleteVehicleAsync(Guid vehicleId);
        Task<IServiceResult> GetActiveVehiclesAsync();
        Task<IServiceResult> GetInactiveVehiclesAsync();
        Task<IServiceResult> UpdateIsActiveAsync(Guid vehicleId, bool isActive);
        Task<IServiceResult> GetVehicleWithHighestBatteryByModelAndStationAsync(Guid vehicleModelId, Guid stationId);
    }
}
