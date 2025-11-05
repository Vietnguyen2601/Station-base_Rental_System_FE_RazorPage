using EVStationRental.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IDamageReportRepository
    {
        Task<List<DamageReport>> GetAllDamageReportsAsync();

        Task<List<DamageReport>> GetDamageReportsByVehicleIdAsync(Guid vehicleId );
        Task<DamageReport?> GetDamageReportByDamageIdAsync(Guid damageId);

        Task<DamageReport?> GetDamageReportByOrderIdAsync(Guid orderId);
        Task<DamageReport> CreateDamageReportAsync(DamageReport damageReport);
        Task<bool> HardDeleteDamageReportAsync(Guid damageId);
        Task<bool> SoftDeleteDamageReportAsync( Guid damageId );

        Task<DamageReport?> UpdateDamageReportAsync(DamageReport damageReport);
    }
}
