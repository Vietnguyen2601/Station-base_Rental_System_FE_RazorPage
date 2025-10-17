using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.DTOs.VehicleTypeDTOs;
using EVStationRental.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.IServices.IVehicleServices
{
    public interface IVehicleTypeServices
    {
        Task<IServiceResult> GetAllVehicleTypesAsync();
        Task<IServiceResult> GetVehicleTypeByIdAsync(Guid vehicleTypeId);
        Task<IServiceResult> CreateVehicleTypeAsync(CreateVehicleTypeRequestDTO dto);
        Task<IServiceResult> UpdateVehicleTypeAsync(Guid vehicleTypeId, UpdateVehicleTypeRequestDTO dto);
        Task<IServiceResult> SoftDeleteVehicleTypeAsync(Guid vehicleTypeId);

        Task<IServiceResult> HardDeleteVehicleTypeAsync(Guid vehicleTypeId);
        Task<IServiceResult> GetAllActiveVehicleTypesAsync();

    }
}
