using System;
using System.Threading.Tasks;
using EVStationRental.Repositories.Models;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IVehicleModelRepository
    {
        Task<List<VehicleModel>> GetAllVehicleModelsAsync();
        Task<VehicleModel?> GetVehicleModelByIdAsync(Guid id);
        Task<List<VehicleModel?>> GetActiveVehicleModelsAsync();
        Task<VehicleModel> CreateVehicleModelAsync(VehicleModel vehicleModel);
        Task<VehicleModel?> UpdateVehicleModelAsync(VehicleModel vehicleModel);
        Task<bool> SoftDeleteVehicleModelAsync(Guid id);
        Task<bool> HardDeleteVehicleModelAsync(Guid id);
    }
}
