using System;
using System.Security.Claims;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVStationRental.APIServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// ??t xe m?i
        /// </summary>
        [HttpPost("book")]
        public async Task<IActionResult> BookVehicle([FromBody] CreateOrderRequestDTO request)
        {
            // Get CustomerId from JWT token
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (customerIdClaim == null)
            {
                return Unauthorized(new { message = "Kh�ng th? x�c th?c ng??i d�ng" });
            }

            if (!Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "ID ng??i d�ng kh�ng h?p l?" });
            }

            var result = await _orderService.CreateOrderAsync(customerId, request);

            return result.StatusCode switch
            {
                200 or 201 => Ok(result),
                400 => BadRequest(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// L?y th�ng tin ??n ??t xe theo ID
        /// </summary>
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var result = await _orderService.GetOrderByIdAsync(orderId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// L?y danh s�ch ??n ??t xe c?a kh�ch h�ng hi?n t?i
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (customerIdClaim == null)
            {
                return Unauthorized(new { message = "Kh�ng th? x�c th?c ng??i d�ng" });
            }

            if (!Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "ID ng??i d�ng kh�ng h?p l?" });
            }

            var result = await _orderService.GetOrdersByCustomerIdAsync(customerId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// H?y ??n ??t xe
        /// </summary>
        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId)
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (customerIdClaim == null)
            {
                return Unauthorized(new { message = "Kh�ng th? x�c th?c ng??i d�ng" });
            }

            if (!Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "ID ng??i d�ng kh�ng h?p l?" });
            }

            var result = await _orderService.CancelOrderAsync(orderId, customerId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                403 => StatusCode(403, result),
                404 => NotFound(result),
                400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
