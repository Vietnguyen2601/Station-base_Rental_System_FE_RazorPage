using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Services.Base;
using System;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.IServices.IOrderServices
{
    public interface IOrderService
    {
        Task<IServiceResult> CreateOrderAsync(Guid customerId, CreateOrderRequestDTO request);
        Task<IServiceResult> GetOrderByIdAsync(Guid orderId);
        Task<IServiceResult> GetOrdersByCustomerIdAsync(Guid customerId);
        Task<IServiceResult> CancelOrderAsync(Guid orderId, Guid customerId);
    }
}
