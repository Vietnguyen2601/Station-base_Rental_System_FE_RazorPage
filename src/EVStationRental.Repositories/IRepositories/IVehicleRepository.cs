using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.Base;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        Task<List<Vehicle>> GetAllVehiclesAsync();
        Task<Vehicle?> GetVehicleByIdAsync(Guid vehicleId);
        Task<Vehicle> CreateVehicleAsync(Vehicle vehicle);
        Task<Vehicle?> UpdateVehicleAsync(Vehicle vehicle);
        Task<bool> SoftDeleteVehicleAsync(Guid vehicleId);
        Task<List<Vehicle>> GetActiveVehiclesAsync();
        Task<List<Vehicle>> GetInactiveVehiclesAsync();
        Task<bool> UpdateIsActiveAsync(Guid vehicleId, bool isActive);
    }
}
