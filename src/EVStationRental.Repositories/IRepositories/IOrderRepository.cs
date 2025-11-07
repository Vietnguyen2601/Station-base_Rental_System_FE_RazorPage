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
        Task<Guid> CreateOrderWithDepositUsingWalletAsync(
            Guid customerId,
            Guid vehicleId,
            DateTime orderDate,
            DateTime startTime,
            DateTime endTime,
            decimal basePrice,
            decimal totalPrice,
            decimal depositAmount,
            string paymentMethod,
            Guid? promotionId = null,
            Guid? staffId = null);

        Task<decimal> FinalizeReturnPaymentUsingWalletAsync(
            Guid orderId,
            string finalPaymentMethod = "WALLET");

        Task<Order?> GetOrderByCodeAsync(string orderCode);
    }
}
