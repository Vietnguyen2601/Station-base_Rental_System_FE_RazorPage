using EVStationRental.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IVehicleTypeRepository
    {
        Task<List<VehicleType>> GetAllActiveVehicleTypesAsync();
        Task<List<VehicleType>> GetAllVehicleTypesAsync();

        Task<VehicleType?> GetVehicleTypeByIdAsync(Guid vehicleTypeId);
        Task<VehicleType> CreateVehicleTypeAsync(VehicleType vehicleType);
        Task<VehicleType?> UpdateVehicleTypeAsync(VehicleType vehicleType);
        Task<bool> HardDeleteVehicleTypeAsync(Guid vehicleTypeId);
        Task<bool> SoftDeleteVehicleTypeAsync(Guid vehicleTypeId);


    }
}
