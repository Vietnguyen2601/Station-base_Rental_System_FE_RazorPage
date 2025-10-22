using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVStationRental.Repositories.Models;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IStationRepository
    {
        Task<Station?> GetStationByIdAsync(Guid id);
        Task<Station> CreateStationAsync(Station station);
        Task<List<Station>> GetAllStationsAsync();
        Task<List<Vehicle>> GetVehiclesByStationIdAsync(Guid stationId);
        Task<Station?> UpdateStationAsync(Station station);
        Task<bool> AddVehiclesToStationAsync(Guid stationId, List<Guid> vehicleIds);
        Task<bool> SoftDeleteStationAsync(Guid stationId);
        Task<List<Station>> GetActiveStationsAsync();
        Task<List<Station>> GetInactiveStationsAsync();
        Task<bool> UpdateIsActiveAsync(Guid stationId, bool isActive);
        Task<List<(Station Station, int AvailableVehicleCount)>> GetStationsByVehicleModelAsync(Guid vehicleModelId);
    }
}
