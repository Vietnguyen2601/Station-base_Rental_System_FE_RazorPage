using EVStationRental.Common.DTOs.PaymentDTOs;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.IServices.IPaymentServices
{
    public interface IPaymentService
    {
        Task<IServiceResult> CreatePaymentAsync(CreatePaymentRequestDTO request);
        Task<IServiceResult> VerifyPaymentAsync(long orderCode);
        Task<IServiceResult> HandleVNPayReturnAsync(VNPayReturnDTO returnData);
        Task<IServiceResult> GetPaymentByOrderIdAsync(Guid orderId);
        Task<IServiceResult> CancelPaymentAsync(Guid paymentId, string reason);
        Task<IServiceResult> RefundDepositAsync(Guid orderId, string reason);
        Task<IServiceResult> ProcessAutoRefundAsync(Guid orderId);
        
        // Stored procedure methods
        Task<IServiceResult> CreateOrderWithDepositAsync(CreateOrderWithDepositDTO request);
        Task<IServiceResult> HandleVNPayReturnWithProcedureAsync(VNPayReturnDTO returnData);
        Task<IServiceResult> CancelOrderWithRefundAsync(Guid orderId, string reason = "Customer cancellation");
        Task<IServiceResult> CompleteOrderWithFinalPaymentAsync(Guid orderId, string finalPaymentMethod = "CASH");

        // Price Calculation Methods - Return decimal values
        Task<decimal> CalculateDepositPriceAsync(Guid orderId);
        Task<decimal> CalculateTotalPriceAsync(Guid orderId);
        Task<decimal> CalculateFinalPriceAsync(Guid orderId);

        // New wallet-based payment flow
        Task<IServiceResult> FinalizeReturnPaymentAsync(FinalizeReturnPaymentDTO request);
    }
}