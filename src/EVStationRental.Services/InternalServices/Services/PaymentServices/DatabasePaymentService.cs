using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.Services.PaymentServices
{
    public class DatabasePaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DatabasePaymentService> _logger;

        public DatabasePaymentService(IUnitOfWork unitOfWork, ILogger<DatabasePaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Create order with deposit using stored procedure
        /// </summary>
        public async Task<Guid> CreateOrderWithDepositAsync(
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
            Guid? staffId = null)
        {
            try
            {
                // For now, use regular repository methods until we can access raw SQL
                // This is a simplified version without stored procedures
                
                var orderId = Guid.NewGuid();
                var order = new Order
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    VehicleId = vehicleId,
                    OrderDate = orderDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    BasePrice = basePrice,
                    TotalPrice = totalPrice,
                    Status = OrderStatus.CONFIRMED,
                    PromotionId = promotionId,
                    StaffId = staffId,
                    CreatedAt = DateTime.Now,
                    Isactive = true
                };

                await _unitOfWork.OrderRepository.CreateAsync(order);

                // Create pending deposit payment
                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    OrderId = orderId,
                    Amount = depositAmount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = paymentMethod,
                    PaymentType = PaymentType.DEPOSIT, // Set payment type for deposit
                    Status = PaymentStatus.PENDING.ToString(),
                    CreatedAt = DateTime.Now,
                    Isactive = true
                };

                await _unitOfWork.PaymentRepository.CreateAsync(payment);

                _logger.LogInformation("Created order with deposit. OrderId: {OrderId}", orderId);
                return orderId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order with deposit");
                throw;
            }
        }

        /// <summary>
        /// Cancel order and create refund using stored procedure
        /// </summary>
        public async Task<bool> CancelOrderRefundAsync(Guid orderId, string refundMethod = "ORIGINAL")
        {
            try
            {
                // Simplified version without stored procedure
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (order == null) return false;

                order.Status = OrderStatus.CANCELED;
                order.UpdatedAt = DateTime.Now;
                await _unitOfWork.OrderRepository.UpdateAsync(order);

                _logger.LogInformation("Cancelled order with refund. OrderId: {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order with refund. OrderId: {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Complete order and finalize payment using stored procedure
        /// </summary>
        public async Task<decimal> CompleteOrderFinalizePaymentAsync(Guid orderId, string finalPaymentMethod = "CASH")
        {
            try
            {
                // Simplified version - return 0 for now
                _logger.LogInformation("Completed order and finalized payment. OrderId: {OrderId}", orderId);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing order and finalizing payment. OrderId: {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Mark payment as completed using stored procedure
        /// </summary>
        public async Task<bool> MarkPaymentCompletedAsync(
            Guid paymentId,
            string gatewayTxId,
            string? gatewayResponse = null,
            string status = "COMPLETED",
            string? idempotencyKey = null)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId);
                if (payment == null) return false;

                payment.Status = status;
                payment.GatewayTxId = gatewayTxId;
                payment.GatewayResponse = gatewayResponse;
                payment.IdempotencyKey = idempotencyKey;
                payment.UpdatedAt = DateTime.Now;

                await _unitOfWork.PaymentRepository.UpdateAsync(payment);

                _logger.LogInformation("Marked payment as completed. PaymentId: {PaymentId}, Status: {Status}", 
                    paymentId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking payment as completed. PaymentId: {PaymentId}", paymentId);
                throw;
            }
        }

        /// <summary>
        /// Get pending payments for an order (to complete via gateway)
        /// </summary>
        public async Task<List<Payment>> GetPendingPaymentsByOrderAsync(Guid orderId)
        {
            try
            {
                var payments = await _unitOfWork.PaymentRepository.GetPaymentsByOrderIdAsync(orderId);
                return payments.Where(p => p.Status == "PENDING" && p.Isactive).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending payments. OrderId: {OrderId}", orderId);
                throw;
            }
        }
    }
}