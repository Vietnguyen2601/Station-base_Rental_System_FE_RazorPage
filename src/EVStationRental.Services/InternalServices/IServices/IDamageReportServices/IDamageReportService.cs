using EVStationRental.Common.DTOs.DamageReportDTOs;
using EVStationRental.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.IServices.IDamageReportServices
{
    public interface IDamageReportService
    {
        Task<IServiceResult> GetAllDamageReports();
        Task<IServiceResult> GetDamageReportByIdAsync(Guid damageReportId);
        Task<IServiceResult> GetDamageReportByOrderIdAsync(Guid orderId);
        Task<IServiceResult> GetDamageReportsByVehicleIdAsync(Guid vehicleId);

        Task<IServiceResult> CreateDamageReportAsync(CreateDamageReportRequestDTO dto);
        Task<IServiceResult> UpdateDamageReportAsync(Guid DamageReportId, UpdateDamageReportRequestDTO dto);
        Task<IServiceResult> SoftDeleteDamageReportAsync(Guid DamageReportId);

        Task<IServiceResult> HardDeleteDamageReportAsync(Guid DamageReportId);

    }
}
