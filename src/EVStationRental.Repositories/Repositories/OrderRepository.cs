using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Repositories.Base;
using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace EVStationRental.Repositories.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        private readonly ElectricVehicleDBContext _context;

        public OrderRepository(ElectricVehicleDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Vehicle)
                    .ThenInclude(v => v.Model)
                .Include(o => o.Promotion)
                .Include(o => o.Staff)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<Order>> GetOrdersByCustomerIdAsync(Guid customerId)
        {
            return await _context.Orders
                .Include(o => o.Vehicle)
                    .ThenInclude(v => v.Model)
                .Include(o => o.Promotion)
                .Where(o => o.CustomerId == customerId && o.Isactive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            order.CreatedAt = DateTime.Now;
            order.Isactive = true;
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> UpdateOrderAsync(Order order)
        {
            // Use AsTracking to enable change tracking for this query
            var existingOrder = await _context.Orders
                .AsTracking()
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
                
            if (existingOrder == null)
            {
                return null;
            }

            existingOrder.EndTime = order.EndTime;
            existingOrder.TotalPrice = order.TotalPrice;
            existingOrder.PromotionId = order.PromotionId;
            existingOrder.StaffId = order.StaffId;
            existingOrder.Status = order.Status;
            existingOrder.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return existingOrder;
        }

        public async Task<bool> IsVehicleAvailableAsync(Guid vehicleId, DateTime startTime, DateTime endTime)
        {
            // Check if vehicle exists and is available
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

            if (vehicle == null || vehicle.Status != VehicleStatus.AVAILABLE)
            {
                return false;
            }

            // Check for overlapping orders with statuses that would block availability
            var hasConflict = await _context.Orders
                .AnyAsync(o =>
                    o.VehicleId == vehicleId &&
                    o.Isactive &&
                    (o.Status == OrderStatus.PENDING || 
                     o.Status == OrderStatus.CONFIRMED || 
                     o.Status == OrderStatus.ONGOING) &&
                    o.StartTime < endTime &&
                    (o.EndTime == null || o.EndTime > startTime));

            return !hasConflict;
        }

        public async Task<List<Order>> GetActiveOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Vehicle)
                    .ThenInclude(v => v.Model)
                .Where(o => o.Isactive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByVehicleIdAsync(Guid vehicleId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Promotion)
                .Where(o => o.VehicleId == vehicleId && o.Isactive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}
