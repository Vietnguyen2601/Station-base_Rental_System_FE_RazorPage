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
        Task<IServiceResult> GetOrderByOrderCodeAsync(string orderCode);
        Task<IServiceResult> GetOrdersByCustomerIdAsync(Guid customerId);
        Task<IServiceResult> CancelOrderAsync(Guid orderId, Guid customerId);
        Task<IServiceResult> GetAllOrdersAsync();
        Task<IServiceResult> StartOrderAsync(Guid orderId);
        Task<IServiceResult> UpdateReturnTimeAsync(Guid orderId);

        // New methods for wallet-based order flow
        Task<IServiceResult> CreateOrderWithWalletDepositAsync(Guid customerId, CreateOrderWithWalletDTO request);
        Task<IServiceResult> EstimateOrderPriceAsync(Guid vehicleId, DateTime startTime, DateTime endTime, string? promotionCode);
        Task<IServiceResult> VerifyOrderCodeAsync(string orderCode);
        Task<IServiceResult> ConfirmOrderByStaffAsync(Guid orderId, Guid staffId);
    }
}
