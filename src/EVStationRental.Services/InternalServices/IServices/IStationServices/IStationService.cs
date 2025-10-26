using System;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.StationDTOs;
using EVStationRental.Services.Base;

namespace EVStationRental.Services.InternalServices.IServices.IStationServices
{
    public interface IStationService
    {
        Task<IServiceResult> CreateStationAsync(CreateStationRequestDTO dto);
        Task<IServiceResult> GetAllStationsAsync();
        Task<IServiceResult> GetVehiclesByStationIdAsync(Guid stationId);
        Task<IServiceResult> AddVehiclesToStationAsync(AddVehiclesToStationRequestDTO dto);
        Task<IServiceResult> UpdateStationAsync(Guid stationId, UpdateStationRequestDTO dto);
        Task<IServiceResult> SoftDeleteStationAsync(Guid stationId);
        Task<IServiceResult> GetActiveStationsAsync();
        Task<IServiceResult> GetInactiveStationsAsync();
        Task<IServiceResult> UpdateIsActiveAsync(Guid stationId, bool isActive);
        Task<IServiceResult> GetStationsByVehicleModelAsync(Guid vehicleModelId);
    }
}
