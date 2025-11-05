using EVStationRental.Common.DTOs.WalletDTOs;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EVStationRental.APIServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Get wallet balance for current user
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null || !Guid.TryParse(accountIdClaim.Value, out var accountId))
            {
                return Unauthorized(new { Message = "Invalid authentication token" });
            }

            var result = await _walletService.GetWalletBalanceAsync(accountId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Top up wallet (nạp tiền vào ví)
        /// </summary>
        [HttpPost("top-up")]
        public async Task<IActionResult> TopUpWallet([FromBody] TopUpWalletDTO request)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null || !Guid.TryParse(accountIdClaim.Value, out var accountId))
            {
                return Unauthorized(new { Message = "Invalid authentication token" });
            }

            var result = await _walletService.TopUpWalletAsync(accountId, request);

            return result.StatusCode switch
            {
                201 => Created("", result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Get wallet transaction history
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactionHistory(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 20)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null || !Guid.TryParse(accountIdClaim.Value, out var accountId))
            {
                return Unauthorized(new { Message = "Invalid authentication token" });
            }

            // Validate pagination
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _walletService.GetTransactionHistoryAsync(accountId, pageNumber, pageSize);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Create wallet for current user (if not exists)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null || !Guid.TryParse(accountIdClaim.Value, out var accountId))
            {
                return Unauthorized(new { Message = "Invalid authentication token" });
            }

            var result = await _walletService.CreateWalletForAccountAsync(accountId);

            return result.StatusCode switch
            {
                201 => Created("", result),
                409 => Conflict(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Create VNPay payment URL by WalletId
        /// Returns VNPay URL to redirect user for payment
        /// </summary>
        [HttpPost("create-vnpay-url")]
        public async Task<IActionResult> CreateVNPayUrl([FromBody] CreateVNPayUrlByWalletDTO request)
        {
            // Get client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _walletService.CreateVNPayUrlByWalletIdAsync(
                request.WalletId,
                request.Amount,
                request.ReturnUrl,
                request.CancelUrl,
                ipAddress
            );

            return result.StatusCode switch
            {
                201 => Created("", result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
