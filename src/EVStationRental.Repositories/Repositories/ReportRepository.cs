using EVStationRental.Repositories.Base;
using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace EVStationRental.Repositories.Repositories
{
    public class ReportRepository : GenericRepository<Report>, IReportRepository
    {
        public ReportRepository(ElectricVehicleDBContext context) : base(context)
        {
        }

        public new async Task<List<Report>> GetAllAsync()
        {
            return await _context.Reports
                .Include(r => r.Account)
                    .ThenInclude(a => a.Role)
                .Include(r => r.Vehicle)
                .Where(r => r.Isactive)
                .ToListAsync();
        }

        public new async Task<Report?> GetByIdAsync(Guid id)
        {
            return await _context.Reports
                .Include(r => r.Account)
                    .ThenInclude(a => a.Role)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.ReportId == id && r.Isactive);
        }

        public async Task<List<Report>> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.Reports
                .Include(r => r.Account)
                    .ThenInclude(a => a.Role)
                .Include(r => r.Vehicle)
                .Where(r => r.AccountId == accountId && r.Isactive)
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();
        }

        public async Task<Order?> GetLatestOrderByCustomerAndVehicleAsync(Guid customerId, Guid vehicleId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId && o.VehicleId == vehicleId && o.Isactive)
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasCustomerRentedVehicleAsync(Guid customerId, Guid vehicleId)
        {
            return await _context.Orders
                .AnyAsync(o => o.CustomerId == customerId && o.VehicleId == vehicleId && o.Isactive);
        }
    }
}
