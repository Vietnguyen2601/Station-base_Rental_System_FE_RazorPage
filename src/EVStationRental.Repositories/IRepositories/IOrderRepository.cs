using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVStationRental.Repositories.Base;
using EVStationRental.Repositories.Models;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetOrderByIdAsync(Guid orderId);
        Task<List<Order>> GetOrdersByCustomerIdAsync(Guid customerId);
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> UpdateOrderAsync(Order order);
        Task<bool> IsVehicleAvailableAsync(Guid vehicleId, DateTime startTime, DateTime endTime);
        Task<List<Order>> GetActiveOrdersAsync();
        Task<List<Order>> GetOrdersByVehicleIdAsync(Guid vehicleId);
        Task<List<Order>> GetAllOrdersAsync();
    }
}
