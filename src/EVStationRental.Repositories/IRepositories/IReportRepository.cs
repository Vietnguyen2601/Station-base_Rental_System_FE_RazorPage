using EVStationRental.Repositories.Base;
using EVStationRental.Repositories.Models;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IReportRepository : IGenericRepository<Report>
    {
        Task<List<Report>> GetByAccountIdAsync(Guid accountId);
        Task<Order?> GetLatestOrderByCustomerAndVehicleAsync(Guid customerId, Guid vehicleId);
        Task<bool> HasCustomerRentedVehicleAsync(Guid customerId, Guid vehicleId);
    }
}
