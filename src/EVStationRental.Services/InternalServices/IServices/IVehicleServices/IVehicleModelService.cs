using EVStationRental.Common.DTOs.VehicleModelDTOs;
using EVStationRental.Repositories.Models;
using EVStationRental.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.IServices.IVehicleServices
{
    public interface IVehicleModelService
    {
        Task<IServiceResult> GetAllVehicleModelsAsync();
        Task<IServiceResult?> GetVehicleModelByIdAsync(Guid id);
        Task<IServiceResult> GetActiveVehicleModelsAsync();
        Task<IServiceResult> CreateVehicleModelAsync(CreateVehicleModelRequestDTO dto);
        Task<IServiceResult> HardDeleteVehicleModelAsync(Guid id);
        Task<IServiceResult> SoftDeleteVehicleModelAsync(Guid id);
        Task<IServiceResult?> UpdateVehicleModelAsync(Guid id, UpdateVehicleModelRequestDTO dto); 
    }
}
