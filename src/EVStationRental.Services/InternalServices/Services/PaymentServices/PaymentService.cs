using EVStationRental.Common.DTOs.PaymentDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.ExternalService.IServices;
using EVStationRental.Services.InternalServices.IServices.IPaymentServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.Services.PaymentServices
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVNPayService _vnPayService;
        private readonly DatabasePaymentService _dbPaymentService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork unitOfWork, 
            IVNPayService vnPayService, 
            DatabasePaymentService dbPaymentService,
            ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _vnPayService = vnPayService;
            _dbPaymentService = dbPaymentService;
            _logger = logger;
        }

        public async Task<IServiceResult> CreatePaymentAsync(CreatePaymentRequestDTO request)
        {
            try
            {
                // Check if order exists
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(request.OrderId);
                if (order == null)
                {
                    return new ServiceResult(404, "Order not found");
                }

                // Generate unique transaction reference
                var txnRef = $"{request.OrderId}_{DateTime.Now.Ticks}";

                // Create VNPay request
                var vnPayRequest = new VNPayRequestDTO
                {
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    OrderInfo = request.Description ?? $"Thanh toan don hang {request.OrderId}",
                    ReturnUrl = request.ReturnUrl,
                    CancelUrl = request.CancelUrl,
                    IpAddress = "127.0.0.1" // Will be updated from controller
                };

                // Create payment URL with VNPay
                var paymentUrl = _vnPayService.CreatePaymentUrl(vnPayRequest, vnPayRequest.IpAddress ?? "127.0.0.1");
                if (string.IsNullOrEmpty(paymentUrl))
                {
                    return new ServiceResult(Const.ERROR_EXCEPTION, "Failed to create VNPay payment URL");
                }

                // Create payment record in database
                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    PaymentMethod = PaymentMethod.VNPAY.ToString(),
                    PaymentType = PaymentType.DEPOSIT,
                    Status = PaymentStatus.PENDING.ToString(),
                    GatewayTxId = txnRef,
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.Now, // Use local time instead of UTC
                    UpdatedAt = DateTime.Now, // Use local time instead of UTC
                    Isactive = true
                };

                await _unitOfWork.PaymentRepository.CreateAsync(payment);

                var response = new PaymentResponseDTO
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    PaymentMethod = PaymentMethod.VNPAY,
                    Status = PaymentStatus.PENDING,
                    GatewayTxId = payment.GatewayTxId,
                    PaymentUrl = paymentUrl,
                    CreatedAt = payment.CreatedAt
                };

                _logger.LogInformation("VNPay payment created successfully. PaymentId: {PaymentId}, OrderId: {OrderId}", 
                    payment.PaymentId, request.OrderId);

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Payment created successfully", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for Order {OrderId}", request.OrderId);
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Error creating payment: {ex.Message}");
            }
        }

        public async Task<IServiceResult> VerifyPaymentAsync(long orderCode)
        {
            try
            {
                // For VNPay, verification is handled via return URL
                // This method can be used to check payment status by transaction reference
                var payment = await _unitOfWork.PaymentRepository.GetByGatewayTxIdAsync(orderCode.ToString());
                if (payment == null)
                {
                    return new ServiceResult(404, "Payment not found in database");
                }

                var response = new PaymentResponseDTO
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    PaymentMethod = Enum.Parse<PaymentMethod>(payment.PaymentMethod),
                    Status = Enum.Parse<PaymentStatus>(payment.Status),
                    GatewayTxId = payment.GatewayTxId,
                    CreatedAt = payment.CreatedAt,
                    PaymentDate = payment.PaymentDate
                };

                _logger.LogInformation("Payment verification completed. PaymentId: {PaymentId}", payment.PaymentId);
                return new ServiceResult(Const.SUCCESS_READ_CODE, "Payment verification completed", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment. OrderCode: {OrderCode}", orderCode);
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Error verifying payment: {ex.Message}");
            }
        }

        public async Task<IServiceResult> HandleVNPayReturnAsync(VNPayReturnDTO returnData)
        {
            try
            {
                // Process VNPay return URL
                var vnPayResult = _vnPayService.ProcessReturnUrl(returnData);
                if (vnPayResult.StatusCode != Const.SUCCESS_PAYMENT_CODE)
                {
                    return vnPayResult;
                }

                // Extract order reference to find payment
                var orderRef = returnData.vnp_TxnRef;
                var payment = await _unitOfWork.PaymentRepository.GetByGatewayTxIdAsync(orderRef);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for TxnRef: {TxnRef}", orderRef);
                    return new ServiceResult(404, "Payment not found");
                }

                // Update payment status
                payment.Status = PaymentStatus.COMPLETED.ToString();
                payment.PaymentDate = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;
                payment.GatewayResponse = System.Text.Json.JsonSerializer.Serialize(returnData);

                // Update order status
                if (payment.Order != null && payment.Order.Status == OrderStatus.PENDING)
                {
                    payment.Order.Status = OrderStatus.CONFIRMED;
                    payment.Order.UpdatedAt = DateTime.Now;
                }

                await _unitOfWork.PaymentRepository.UpdateAsync(payment);

                _logger.LogInformation("VNPay return processed successfully. PaymentId: {PaymentId}", payment.PaymentId);

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Payment completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay return");
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Error processing return: {ex.Message}");
            }
        }

        public async Task<IServiceResult> GetPaymentByOrderIdAsync(Guid orderId)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRepository.GetLatestPaymentByOrderIdAsync(orderId);
                if (payment == null)
                {
                    return new ServiceResult(404, "Payment not found");
                }

                var response = new PaymentResponseDTO
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    PaymentMethod = Enum.Parse<PaymentMethod>(payment.PaymentMethod),
                    Status = Enum.Parse<PaymentStatus>(payment.Status),
                    GatewayTxId = payment.GatewayTxId,
                    CreatedAt = payment.CreatedAt,
                    PaymentDate = payment.PaymentDate
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Payment retrieved successfully", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment for Order {OrderId}", orderId);
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Error retrieving payment: {ex.Message}");
            }
        }

        public async Task<IServiceResult> CancelPaymentAsync(Guid paymentId, string reason)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return new ServiceResult(404, "Payment not found");
                }

                if (payment.Status == PaymentStatus.COMPLETED.ToString())
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Cannot cancel completed payment");
                }

                // VNPay doesn't support direct payment cancellation via API
                // Payment will expire automatically if not completed within the time limit

                // Update payment status
                payment.Status = PaymentStatus.CANCELED.ToString();
                payment.UpdatedAt = DateTime.Now;

                await _unitOfWork.PaymentRepository.UpdateAsync(payment);

                _logger.LogInformation("Payment cancelled successfully. PaymentId: {PaymentId}", paymentId);

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Payment cancelled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Error cancelling payment: {ex.Message}");
            }
        }

        /// <summary>
        /// Hoàn cọc thủ công (khi hủy đơn)
        /// </summary>
        public async Task<IServiceResult> RefundDepositAsync(Guid orderId, string reason)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRepository.GetByOrderIdAsync(orderId);
                if (payment == null)
                {
                    return new ServiceResult(404, "Payment not found for this order");
                }

                if (payment.Status != PaymentStatus.COMPLETED.ToString())
                {
                    return new ServiceResult(400, "Cannot refund payment that is not completed");
                }

                // VNPay không hỗ trợ hoàn tiền tự động qua API trong sandbox
                // Trong thực tế sẽ cần tích hợp API hoàn tiền hoặc xử lý thủ công
                
                // Tạo bản ghi hoàn tiền
                var refundPayment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    OrderId = orderId,
                    Amount = -payment.Amount, // Số âm để đánh dấu hoàn tiền
                    Status = PaymentStatus.COMPLETED.ToString(),
                    PaymentMethod = "VNPAY_REFUND",
                    PaymentType = PaymentType.REFUND, // Set payment type for refund
                    GatewayTxId = $"REFUND_{payment.GatewayTxId}",
                    GatewayResponse = $"{{\"reason\": \"{reason}\", \"refund_type\": \"manual\"}}",
                    PaymentDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Isactive = true
                };

                await _unitOfWork.PaymentRepository.CreateAsync(refundPayment);

                // Cập nhật trạng thái đơn hàng nếu cần
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (order != null)
                {
                    order.Status = OrderStatus.REFUNDED;
                    order.UpdatedAt = DateTime.Now;
                    await _unitOfWork.OrderRepository.UpdateAsync(order);
                }

                _logger.LogInformation("Deposit refunded successfully. OrderId: {OrderId}, RefundAmount: {Amount}", 
                    orderId, payment.Amount);

                return new ServiceResult(200, "Deposit refunded successfully", new
                {
                    RefundPaymentId = refundPayment.PaymentId,
                    RefundAmount = payment.Amount,
                    Reason = reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding deposit for order {OrderId}", orderId);
                return new ServiceResult(500, $"Error refunding deposit: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý hoàn cọc tự động khi hoàn thành đơn hàng
        /// </summary>
        public async Task<IServiceResult> ProcessAutoRefundAsync(Guid orderId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return new ServiceResult(404, "Order not found");
                }

                if (order.Status != OrderStatus.COMPLETED)
                {
                    return new ServiceResult(400, "Cannot process auto refund for non-completed order");
                }

                var payment = await _unitOfWork.PaymentRepository.GetByOrderIdAsync(orderId);
                if (payment == null)
                {
                    return new ServiceResult(404, "Payment not found for this order");
                }

                // Kiểm tra xem đã hoàn cọc chưa
                var existingRefunds = await _unitOfWork.PaymentRepository
                    .GetPaymentsByOrderIdAsync(orderId);
                
                var hasRefund = existingRefunds.Any(p => p.Amount < 0); // Số âm = hoàn tiền
                if (hasRefund)
                {
                    return new ServiceResult(400, "Deposit has already been refunded");
                }

                // Tạo bản ghi hoàn cọc tự động
                var autoRefund = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    OrderId = orderId,
                    Amount = -payment.Amount, // Hoàn lại toàn bộ cọc
                    Status = PaymentStatus.COMPLETED.ToString(),
                    PaymentMethod = "VNPAY_AUTO_REFUND",
                    PaymentType = PaymentType.FINAL, // Set payment type for final payment
                    GatewayTxId = $"AUTO_REFUND_{payment.GatewayTxId}",
                    GatewayResponse = "{\"refund_type\": \"auto\", \"trigger\": \"order_completed\"}",
                    PaymentDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Isactive = true
                };

                await _unitOfWork.PaymentRepository.CreateAsync(autoRefund);

                _logger.LogInformation("Auto refund processed successfully. OrderId: {OrderId}, RefundAmount: {Amount}", 
                    orderId, payment.Amount);

                return new ServiceResult(200, "Auto refund processed successfully", new
                {
                    RefundPaymentId = autoRefund.PaymentId,
                    RefundAmount = payment.Amount,
                    RefundType = "auto"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing auto refund for order {OrderId}", orderId);
                return new ServiceResult(500, $"Error processing auto refund: {ex.Message}");
            }
        }

        /// <summary>
        /// Create order with deposit using stored procedure (NEW APPROACH)
        /// </summary>
        public async Task<IServiceResult> CreateOrderWithDepositAsync(CreateOrderWithDepositDTO request)
        {
            try
            {
                // Use stored procedure to create order and deposit payment
                var orderId = await _dbPaymentService.CreateOrderWithDepositAsync(
                    request.CustomerId,
                    request.VehicleId,
                    request.OrderDate,
                    request.StartTime,
                    request.EndTime,
                    request.BasePrice,
                    request.TotalPrice,
                    request.DepositAmount,
                    "VNPAY", // Payment method
                    request.PromotionId,
                    request.StaffId
                );

                // Get the pending deposit payment created by the procedure
                var pendingPayments = await _dbPaymentService.GetPendingPaymentsByOrderAsync(orderId);
                var depositPayment = pendingPayments.FirstOrDefault(p => p.PaymentMethod == "VNPAY");

                if (depositPayment == null)
                {
                    return new ServiceResult(500, "Failed to create deposit payment");
                }

                // Create VNPay payment URL
                var vnPayRequest = new VNPayRequestDTO
                {
                    OrderId = orderId,
                    Amount = request.DepositAmount,
                    OrderInfo = $"Coc dat xe - Don hang {orderId}",
                    ReturnUrl = request.ReturnUrl,
                    TxnRef = depositPayment.PaymentId.ToString()
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(vnPayRequest, request.IpAddress ?? "127.0.0.1");

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    return new ServiceResult(500, "Failed to create VNPay payment URL");
                }

                var response = new PaymentResponseDTO
                {
                    PaymentId = depositPayment.PaymentId,
                    OrderId = orderId,
                    Amount = request.DepositAmount,
                    PaymentUrl = paymentUrl,
                    Status = PaymentStatus.PENDING,
                    Message = "Order created successfully. Please complete deposit payment."
                };

                return new ServiceResult(201, "Order with deposit created successfully", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order with deposit");
                return new ServiceResult(500, $"Error creating order with deposit: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle VNPay return and mark payment completed using stored procedure
        /// </summary>
        public async Task<IServiceResult> HandleVNPayReturnWithProcedureAsync(VNPayReturnDTO returnData)
        {
            try
            {
                // Validate VNPay signature
                if (!_vnPayService.ValidateSignature(returnData))
                {
                    return new ServiceResult(400, "Invalid VNPay signature");
                }

                // Parse payment ID from txn_ref
                if (!Guid.TryParse(returnData.vnp_TxnRef, out var paymentId))
                {
                    return new ServiceResult(400, "Invalid payment reference");
                }

                // Determine status based on VNPay response
                var status = returnData.vnp_ResponseCode == "00" ? "COMPLETED" : "FAILED";
                var gatewayResponse = System.Text.Json.JsonSerializer.Serialize(returnData);

                // Use stored procedure to mark payment as completed
                var success = await _dbPaymentService.MarkPaymentCompletedAsync(
                    paymentId,
                    returnData.vnp_TransactionNo ?? returnData.vnp_TxnRef,
                    gatewayResponse,
                    status,
                    returnData.vnp_TxnRef // Use as idempotency key
                );

                if (!success)
                {
                    return new ServiceResult(500, "Failed to update payment status");
                }

                _logger.LogInformation("VNPay return processed with procedure. PaymentId: {PaymentId}, Status: {Status}", 
                    paymentId, status);

                return new ServiceResult(200, "Payment processed successfully", new { 
                    PaymentId = paymentId, 
                    Status = status,
                    Success = status == "COMPLETED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling VNPay return with procedure");
                return new ServiceResult(500, $"Error processing VNPay return: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancel order with refund using stored procedure
        /// </summary>
        public async Task<IServiceResult> CancelOrderWithRefundAsync(Guid orderId, string reason = "Customer request")
        {
            try
            {
                // Use stored procedure to cancel order and create refund
                var success = await _dbPaymentService.CancelOrderRefundAsync(orderId, "VNPAY");

                if (!success)
                {
                    return new ServiceResult(500, "Failed to cancel order");
                }

                // Get pending refund payments to process via gateway
                var pendingRefunds = await _dbPaymentService.GetPendingPaymentsByOrderAsync(orderId);
                var refundPayments = pendingRefunds.Where(p => p.PaymentMethod == "VNPAY").ToList();

                return new ServiceResult(200, "Order cancelled successfully", new {
                    OrderId = orderId,
                    RefundPayments = refundPayments.Select(p => new {
                        PaymentId = p.PaymentId,
                        Amount = p.Amount,
                        Status = p.Status
                    }),
                    Message = "Refund payments created. Process via gateway to complete."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order with refund. OrderId: {OrderId}", orderId);
                return new ServiceResult(500, $"Error cancelling order: {ex.Message}");
            }
        }

        /// <summary>
        /// Complete order and finalize payment using stored procedure
        /// </summary>
        public async Task<IServiceResult> CompleteOrderWithFinalPaymentAsync(Guid orderId, string finalPaymentMethod = "CASH")
        {
            try
            {
                // Use stored procedure to complete order and calculate final payment
                var dueAmount = await _dbPaymentService.CompleteOrderFinalizePaymentAsync(orderId, finalPaymentMethod);

                var message = dueAmount switch
                {
                    > 0 => $"Order completed. Final payment of {dueAmount:C} required.",
                    < 0 => $"Order completed. Refund of {Math.Abs(dueAmount):C} will be processed.",
                    _ => "Order completed. No additional payment required."
                };

                return new ServiceResult(200, message, new {
                    OrderId = orderId,
                    DueAmount = dueAmount,
                    Status = "COMPLETED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing order with final payment. OrderId: {OrderId}", orderId);
                return new ServiceResult(500, $"Error completing order: {ex.Message}");
            }
        }

        /// <summary>
        /// Tính tiền cọc (Deposit Price) = 10% của base_price
        /// </summary>
        public async Task<decimal> CalculateDepositPriceAsync(Guid orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            // deposit_price = 10% của base_price - làm tròn về số nguyên
            decimal depositPrice = Math.Round(order.BasePrice * 0.10m, 0);

            _logger.LogInformation("Deposit price calculated for Order {OrderId}: {DepositPrice}", 
                orderId, depositPrice);

            return depositPrice;
        }

        /// <summary>
        /// Tính tổng giá (Total Price) = base_price - promotion_price(if any) + damage_cost(if any)
        /// </summary>
        public async Task<decimal> CalculateTotalPriceAsync(Guid orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            decimal basePrice = order.BasePrice;
            decimal promotionPrice = 0m;
            decimal damageCost = 0m;

            // Áp dụng promotion nếu có
            if (order.PromotionId.HasValue)
            {
                var promotion = await _unitOfWork.PromotionRepository.GetByIdAsync(order.PromotionId.Value);
                if (promotion != null && promotion.Isactive)
                {
                    promotionPrice = Math.Round(basePrice * (promotion.DiscountPercentage / 100), 0);
                }
            }

            // Lấy damage cost nếu có damage report cho order này
            var damageReports = await _unitOfWork.DamageReportRepository.GetAllDamageReportsAsync();
            var orderDamageReport = damageReports.FirstOrDefault(dr => dr.OrderId == orderId && dr.Isactive);
            if (orderDamageReport != null)
            {
                damageCost = orderDamageReport.EstimatedCost;
            }

            // Total = base_price - promotion_price(if any) + damage_cost(if any) - làm tròn
            decimal totalPrice = Math.Round(basePrice - promotionPrice + damageCost, 0);

            _logger.LogInformation("Total price calculated for Order {OrderId}: {TotalPrice} (Base: {BasePrice}, Promotion: {PromotionPrice}, Damage: {DamageCost})", 
                orderId, totalPrice, basePrice, promotionPrice, damageCost);

            return totalPrice;
        }

        /// <summary>
        /// Tính giá cuối cùng (Final Price) = base_price - deposit_price - promotion_price(if any) + damage_cost(if any)
        /// </summary>
        public async Task<decimal> CalculateFinalPriceAsync(Guid orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            decimal basePrice = order.BasePrice;
            decimal depositPrice = Math.Round(basePrice * 0.10m, 0); // 10% deposit - làm tròn
            decimal promotionPrice = 0m;
            decimal damageCost = 0m;

            // Áp dụng promotion nếu có
            if (order.PromotionId.HasValue)
            {
                var promotion = await _unitOfWork.PromotionRepository.GetByIdAsync(order.PromotionId.Value);
                if (promotion != null && promotion.Isactive)
                {
                    promotionPrice = Math.Round(basePrice * (promotion.DiscountPercentage / 100), 0);
                }
            }

            // Lấy damage cost nếu có damage report cho order này
            var damageReports = await _unitOfWork.DamageReportRepository.GetAllDamageReportsAsync();
            var orderDamageReport = damageReports.FirstOrDefault(dr => dr.OrderId == orderId && dr.Isactive);
            if (orderDamageReport != null)
            {
                damageCost = orderDamageReport.EstimatedCost;
            }

            // Final = base_price - deposit_price - promotion_price(if any) + damage_cost(if any) - làm tròn
            decimal finalPrice = Math.Round(basePrice - depositPrice - promotionPrice + damageCost, 0);

            _logger.LogInformation("Final price calculated for Order {OrderId}: {FinalPrice} (Base: {BasePrice}, Deposit: {DepositPrice}, Promotion: {PromotionPrice}, Damage: {DamageCost})", 
                orderId, finalPrice, basePrice, depositPrice, promotionPrice, damageCost);

            return finalPrice;
        }

        /// <summary>
        /// Finalize return payment when customer returns vehicle
        /// </summary>
        public async Task<IServiceResult> FinalizeReturnPaymentAsync(FinalizeReturnPaymentDTO request)
        {
            try
            {
                // Find the ongoing order for the customer
                var orders = await _unitOfWork.OrderRepository.GetOrdersByCustomerIdAsync(request.AccountId);
                var order = orders?.FirstOrDefault(o => o.Status == OrderStatus.ONGOING);
                
                if (order == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm thấy đơn hàng đang thuê của khách hàng"
                    };
                }

                if (order.Status == OrderStatus.COMPLETED)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Đơn hàng đã được hoàn thành trước đó"
                    };
                }

                if (order.Status != OrderStatus.ONGOING)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = $"Không thể hoàn tất thanh toán cho đơn hàng có trạng thái {order.Status}"
                    };
                }

                // Use the amount provided in the request
                var amountToDeduct = request.Amount;

                // Handle payment if using WALLET
                if (request.FinalPaymentMethod.ToUpper() == "WALLET" && amountToDeduct > 0)
                {
                    // Get customer wallet
                    var wallet = await _unitOfWork.WalletRepository.GetByAccountIdAsync(request.AccountId);
                    if (wallet == null)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.WARNING_NO_DATA_CODE,
                            Message = "Không tìm thấy ví của khách hàng"
                        };
                    }

                    if (wallet.Balance < amountToDeduct)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.ERROR_VALIDATION_CODE,
                            Message = $"Số dư ví không đủ. Cần: {amountToDeduct:N0} VNĐ, Có: {wallet.Balance:N0} VNĐ"
                        };
                    }

                    // Deduct from wallet
                    wallet.Balance -= amountToDeduct;
                    wallet.UpdatedAt = DateTime.Now;
                    await _unitOfWork.WalletRepository.UpdateWalletAsync(wallet);

                    // Create wallet transaction
                    var walletTransaction = new WalletTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        WalletId = wallet.WalletId,
                        OrderId = order.OrderId,
                        Amount = -amountToDeduct, // Negative for deduction
                        TransactionType = TransactionType.PAYMENT,
                        Description = $"Final payment for order {order.OrderCode}",
                        CreatedAt = DateTime.Now,
                        Isactive = true
                    };
                    await _unitOfWork.WalletRepository.CreateTransactionAsync(walletTransaction);

                    // Create payment record
                    var payment = new Payment
                    {
                        PaymentId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        GatewayTxId = $"WALLET-{Guid.NewGuid()}",
                        Amount = amountToDeduct,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "WALLET",
                        PaymentType = PaymentType.FINAL,
                        Status = "COMPLETED",
                        CreatedAt = DateTime.Now,
                        Isactive = true
                    };
                    await _unitOfWork.PaymentRepository.CreateAsync(payment);
                }

                // Update order status to COMPLETED
                order.Status = OrderStatus.COMPLETED;
                order.UpdatedAt = DateTime.Now;
                await _unitOfWork.OrderRepository.UpdateOrderAsync(order);

                // Update vehicle status to AVAILABLE
                if (order.Vehicle != null)
                {
                    order.Vehicle.Status = VehicleStatus.AVAILABLE;
                    order.Vehicle.UpdatedAt = DateTime.Now;
                    await _unitOfWork.VehicleRepository.UpdateVehicleAsync(order.Vehicle);
                }

                var response = new FinalizeReturnPaymentResponseDTO
                {
                    AccountId = request.AccountId,
                    AmountDeducted = amountToDeduct,
                    PaymentMethod = request.FinalPaymentMethod,
                    PaymentStatus = "COMPLETED",
                    CompletedAt = DateTime.Now,
                    Message = $"Đã trừ {amountToDeduct:N0} VNĐ từ ví thành công"
                };

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_UPDATE_CODE,
                    Message = "Hoàn tất thanh toán thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing return payment for customer {AccountId}", request.AccountId);
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi hoàn tất thanh toán: {ex.Message}",
                    Data = new { error = ex.Message, innerError = ex.InnerException?.Message }
                };
            }
        }
    }
}
