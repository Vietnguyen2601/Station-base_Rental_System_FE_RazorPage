using EVStationRental.Common.DTOs.PaymentDTOs;
using EVStationRental.Services.InternalServices.IServices.IPaymentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVStationRental.APIServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Tạo payment cho đơn hàng
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDTO request)
        {
            // Set default returnUrl if not provided
            if (string.IsNullOrEmpty(request.ReturnUrl))
            {
                request.ReturnUrl = $"{Request.Scheme}://{Request.Host}/api/Payment/vnpay-return";
            }

            // Set default cancelUrl if not provided  
            if (string.IsNullOrEmpty(request.CancelUrl))
            {
                request.CancelUrl = $"{Request.Scheme}://{Request.Host}/api/Payment/vnpay-cancel";
            }

            var result = await _paymentService.CreatePaymentAsync(request);

            return result.StatusCode switch
            {
                201 => Created("", result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Lấy thông tin thanh toán theo Order ID
        /// </summary>
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
        /// Test VNPay payment creation (for development)
        /// </summary>
        [HttpPost("test")]
        [AllowAnonymous]
        public async Task<IActionResult> TestPayment([FromBody] CreatePaymentRequestDTO request)
        {
            // Set default returnUrl if not provided
            if (string.IsNullOrEmpty(request.ReturnUrl))
            {
                request.ReturnUrl = $"{Request.Scheme}://{Request.Host}/api/Payment/vnpay-return";
            }

            // Set default cancelUrl if not provided  
            if (string.IsNullOrEmpty(request.CancelUrl))
            {
                request.CancelUrl = $"{Request.Scheme}://{Request.Host}/api/Payment/vnpay-cancel";
            }

            var result = await _paymentService.CreatePaymentAsync(request);

            return result.StatusCode switch
            {
                201 => Created("", result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// VNPay return URL endpoint
        /// </summary>
        [HttpGet("vnpay-return")]
        [AllowAnonymous]
        public async Task<IActionResult> VNPayReturn([FromQuery] VNPayReturnDTO returnData)
        {
            var result = await _paymentService.HandleVNPayReturnAsync(returnData);

            if (result.StatusCode == 200)
            {
                // Redirect to success page or return success response
                return Ok(new { 
                    success = true, 
                    message = "Payment successful",
                    data = result.Data 
                });
            }
            else
            {
                // Return failure response
                return BadRequest(new { 
                    success = false, 
                    message = "Payment failed",
                    data = result.Data 
                });
            }
        }

        /// <summary>
        /// VNPay cancel URL endpoint (when user cancels payment)
        /// </summary>
        [HttpGet("vnpay-cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> VNPayCancel([FromQuery] string? orderId, [FromQuery] string? reason)
        {
            try
            {
                // Log user cancellation
                if (Guid.TryParse(orderId, out var orderGuid))
                {
                    // Optionally update order status to cancelled
                    // await _paymentService.CancelPaymentAsync(orderGuid, reason ?? "User cancelled");
                }

                return Ok(new { 
                    success = false, 
                    message = "Payment was cancelled by user",
                    orderId = orderId,
                    reason = reason ?? "User cancellation"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error processing cancellation",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// VNPay return URL endpoint using stored procedures (IMPROVED)
        /// </summary>
        [HttpGet("vnpay-return-v2")]
        [AllowAnonymous]
        public async Task<IActionResult> VNPayReturnV2([FromQuery] VNPayReturnDTO returnData)
        {
            var result = await _paymentService.HandleVNPayReturnWithProcedureAsync(returnData);

            if (result.StatusCode == 200)
            {
                return Ok(new { 
                    success = true, 
                    message = "Payment processed successfully",
                    data = result.Data 
                });
            }
            else
            {
                return BadRequest(new { 
                    success = false, 
                    message = result.Message,
                    data = result.Data 
                });
            }
        }

        /// <summary>
        /// Create order with deposit using stored procedures (IMPROVED)
        /// </summary>
        [HttpPost("order-with-deposit")]
        [Authorize]
        public async Task<IActionResult> CreateOrderWithDeposit([FromBody] CreateOrderWithDepositDTO request)
        {
            var result = await _paymentService.CreateOrderWithDepositAsync(request);

            return result.StatusCode switch
            {
                201 => Created("", result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Cancel order with refund using stored procedures (IMPROVED)
        /// </summary>
        [HttpPost("cancel-order-with-refund/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CancelOrderWithRefund(Guid orderId, [FromBody] RefundRequestDTO request)
        {
            var result = await _paymentService.CancelOrderWithRefundAsync(orderId, request.Reason);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Complete order with final payment using stored procedures (IMPROVED)  
        /// </summary>
        [HttpPost("complete-order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CompleteOrderWithFinalPayment(Guid orderId)
        {
            var result = await _paymentService.CompleteOrderWithFinalPaymentAsync(orderId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// VNPay webhook endpoint (if needed for server-to-server notifications)
        /// </summary>
        [HttpPost("vnpay-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> VNPayWebhook([FromBody] VNPayReturnDTO returnData)
        {
            var result = await _paymentService.HandleVNPayReturnAsync(returnData);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Hoàn cọc thủ công (khi hủy đơn)
        /// </summary>
        [HttpPost("refund/{orderId}")]
        [Authorize]
        public async Task<IActionResult> RefundDeposit(Guid orderId, [FromBody] RefundRequestDTO request)
        {
            var result = await _paymentService.RefundDepositAsync(orderId, request.Reason);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Xử lý hoàn cọc tự động (khi hoàn thành đơn)
        /// </summary>
        [HttpPost("auto-refund/{orderId}")]
        [Authorize]
        public async Task<IActionResult> ProcessAutoRefund(Guid orderId)
        {
            var result = await _paymentService.ProcessAutoRefundAsync(orderId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Tính tiền cọc (Deposit Price) = 10% của base_price
        /// </summary>
        [HttpGet("calculate-deposit/{orderId}")]
        [AllowAnonymous]
        public async Task<IActionResult> CalculateDepositPrice(Guid orderId)
        {
            try
            {
                var depositPrice = await _paymentService.CalculateDepositPriceAsync(orderId);
                
                return Ok(new
                {
                    statusCode = 200,
                    message = "Deposit price calculated successfully",
                    data = new
                    {
                        orderId = orderId,
                        depositPrice = depositPrice,
                        depositPercentage = 10,
                        message = $"Deposit price is 10% of base price: {depositPrice:C}"
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = ex.Message,
                    data = (object?)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = $"Error calculating deposit price: {ex.Message}",
                    data = (object?)null
                });
            }
        }

        /// <summary>
        /// Tính tổng giá (Total Price) = base_price - promotion_price(if any)
        /// </summary>
        [HttpGet("calculate-total/{orderId}")]
        [AllowAnonymous]
        public async Task<IActionResult> CalculateTotalPrice(Guid orderId)
        {
            try
            {
                var totalPrice = await _paymentService.CalculateTotalPriceAsync(orderId);
                
                return Ok(new
                {
                    statusCode = 200,
                    message = "Total price calculated successfully",
                    data = new
                    {
                        orderId = orderId,
                        totalPrice = totalPrice,
                        message = $"Total price calculated: {totalPrice:C}"
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = ex.Message,
                    data = (object?)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = $"Error calculating total price: {ex.Message}",
                    data = (object?)null
                });
            }
        }

        /// <summary>
        /// Tính giá cuối cùng (Final Price) = base_price - deposit_price - promotion_price(if any)
        /// </summary>
        [HttpGet("calculate-final/{orderId}")]
        [AllowAnonymous]
        public async Task<IActionResult> CalculateFinalPrice(Guid orderId)
        {
            try
            {
                var finalPrice = await _paymentService.CalculateFinalPriceAsync(orderId);
                
                return Ok(new
                {
                    statusCode = 200,
                    message = "Final price calculated successfully",
                    data = new
                    {
                        orderId = orderId,
                        finalPrice = finalPrice,
                        message = $"Final price calculated: {finalPrice:C}"
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = ex.Message,
                    data = (object?)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = $"Error calculating final price: {ex.Message}",
                    data = (object?)null
                });
            }
        }

        /// <summary>
        /// Hoàn tất thanh toán khi trả xe (NEW WALLET-BASED FLOW)
        /// Staff gọi endpoint này khi khách trả xe
        /// </summary>
        [HttpPost("finalize-return")]
        [Authorize(Roles = "STAFF,ADMIN")]
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
    }

    /// <summary>
    /// DTO for refund request
    /// </summary>
    public class RefundRequestDTO
    {
        public string Reason { get; set; } = "Customer request";
    }
}