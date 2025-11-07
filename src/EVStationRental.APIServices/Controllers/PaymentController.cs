using EVStationRental.Common.DTOs.PaymentDTOs;
using EVStationRental.Services.InternalServices.IServices.IPaymentServices;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVStationRental.APIServices.Controllers
{
    /// <summary>
    /// Payment Controller - Handles payment operations for the wallet-based rental system
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IWalletService _walletService;

        public PaymentController(IPaymentService paymentService, IWalletService walletService)
        {
            _paymentService = paymentService;
            _walletService = walletService;
        }

        /// <summary>
        /// Lấy thông tin thanh toán theo Order ID
        /// Get payment information by order ID
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <returns>Payment details including all transactions</returns>
        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentByOrderId(Guid orderId)
        {
            var result = await _paymentService.GetPaymentByOrderIdAsync(orderId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Tính giá cuối cùng (Final Price) cho đơn hàng
        /// Calculate final price for an order
        /// Formula: base_price - deposit_price - promotion_price + damage_cost
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <returns>Final price calculation result</returns>
        [HttpGet("calculate-final-price/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CalculateFinalPrice(Guid orderId)
        {
            try
            {
                var finalPrice = await _paymentService.CalculateFinalPriceAsync(orderId);

                return Ok(new
                {
                    StatusCode = 200,
                    Message = "Tính giá cuối cùng thành công",
                    Data = new
                    {
                        OrderId = orderId,
                        FinalPrice = finalPrice,
                        Currency = "VND"
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    StatusCode = 500,
                    Message = $"Lỗi khi tính giá cuối cùng: {ex.Message}"
                });
            }
        }

        ///// <summary>
        ///// Tính tiền cọc (Deposit Price) cho đơn hàng
        ///// Calculate deposit price for an order (10% of base price)
        ///// </summary>
        ///// <param name="orderId">Order ID</param>
        ///// <returns>Deposit price calculation result</returns>
        //[HttpGet("calculate-deposit-price/{orderId}")]
        //[Authorize]
        //public async Task<IActionResult> CalculateDepositPrice(Guid orderId)
        //{
        //    try
        //    {
        //        var depositPrice = await _paymentService.CalculateDepositPriceAsync(orderId);

        //        return Ok(new
        //        {
        //            StatusCode = 200,
        //            Message = "Tính tiền cọc thành công",
        //            Data = new
        //            {
        //                OrderId = orderId,
        //                DepositPrice = depositPrice,
        //                Currency = "VND"
        //            }
        //        });
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return NotFound(new
        //        {
        //            StatusCode = 404,
        //            Message = ex.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            StatusCode = 500,
        //            Message = $"Lỗi khi tính tiền cọc: {ex.Message}"
        //        });
        //    }
        //}

        ///// <summary>
        ///// Tính tổng giá (Total Price) cho đơn hàng
        ///// Calculate total price for an order
        ///// Formula: base_price - promotion_price + damage_cost
        ///// </summary>
        ///// <param name="orderId">Order ID</param>
        ///// <returns>Total price calculation result</returns>
        //[HttpGet("calculate-total-price/{orderId}")]
        //[Authorize]
        //public async Task<IActionResult> CalculateTotalPrice(Guid orderId)
        //{
        //    try
        //    {
        //        var totalPrice = await _paymentService.CalculateTotalPriceAsync(orderId);

        //        return Ok(new
        //        {
        //            StatusCode = 200,
        //            Message = "Tính tổng giá thành công",
        //            Data = new
        //            {
        //                OrderId = orderId,
        //                TotalPrice = totalPrice,
        //                Currency = "VND"
        //            }
        //        });
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return NotFound(new
        //        {
        //            StatusCode = 404,
        //            Message = ex.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            StatusCode = 500,
        //            Message = $"Lỗi khi tính tổng giá: {ex.Message}"
        //        });
        //    }
        //}

        /// <summary>
        /// Hoàn tất thanh toán khi trả xe (WALLET-BASED FLOW)
        /// Staff calls this endpoint when customer returns the vehicle
        /// - Deducts remaining amount from wallet
        /// - Refunds deposit to wallet
        /// - Updates order status to COMPLETED
        /// </summary>
        /// <param name="request">Contains orderId and actualReturnDate</param>
        /// <returns>Payment completion result with updated balances</returns>
        [HttpPost("finalize-return")]
        public async Task<IActionResult> FinalizeReturnPayment([FromBody] FinalizeReturnPaymentDTO request)
        {
            var result = await _paymentService.FinalizeReturnPaymentAsync(request);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// VNPay return URL endpoint for wallet top-up
        /// VNPay will redirect user here after payment
        /// </summary>
        [HttpGet("wallet-vnpay-return")]
        [AllowAnonymous]
        public async Task<IActionResult> WalletVNPayReturn([FromQuery] VNPayReturnDTO returnData)
        {
            var result = await _walletService.HandleVNPayWalletReturnAsync(returnData);

            if (result.StatusCode == 200)
            {
                // Payment successful - redirect to success page
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                });
            }
            else
            {
                // Payment failed
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    data = result.Data
                });
            }
        }

        /// <summary>
        /// VNPay cancel URL endpoint for wallet top-up
        /// VNPay redirects here when user cancels payment
        /// </summary>
        [HttpGet("wallet-vnpay-cancel")]
        [AllowAnonymous]
        public IActionResult WalletVNPayCancel([FromQuery] string? transactionId)
        {
            return Ok(new
            {
                success = false,
                message = "Người dùng đã hủy thanh toán nạp tiền",
                transactionId = transactionId
            });
        }
    }
}