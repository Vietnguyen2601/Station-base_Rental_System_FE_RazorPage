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