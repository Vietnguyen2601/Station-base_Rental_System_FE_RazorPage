using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
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
        /// 
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var result = await _orderService.GetAllOrdersAsync();
            if (result.Data == null)
            {
                return NotFound(new
                {
                    Message = Const.WARNING_NO_DATA_MSG
                });
            }
            return Ok(new
            {
                Message = Const.SUCCESS_READ_MSG,
                Data = result.Data
            });
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookVehicle([FromBody] CreateOrderRequestDTO request)
        {
            // Get CustomerId from JWT token
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (customerIdClaim == null)
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng" });
            }

            if (!Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "ID người dùng không hợp lệ" });
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
        /// Lấy thông tin đơn đặt xe theo ID
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
        /// Lấy thông tin đơn đặt xe theo OrderCode (Chỉ dành cho nhân viên)
        /// </summary>
        [HttpGet("code/{orderCode}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetOrderByCode(string orderCode)
        {
            var result = await _orderService.GetOrderByOrderCodeAsync(orderCode);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Lấy danh sách đơn đặt xe của khách hàng hiện tại
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (customerIdClaim == null)
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng" });
            }

            if (!Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "ID người dùng không hợp lệ" });
            }

            var result = await _orderService.GetOrdersByCustomerIdAsync(customerId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Hủy đơn đặt xe
        /// </summary>
        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId)
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (customerIdClaim == null)
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng" });
            }

            if (!Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { message = "ID người dùng không hợp lệ" });
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

        /// <summary>
        /// Bắt đầu sử dụng xe (chuyển status sang ONGOING)
        /// </summary>
        [HttpPut("{orderId}/start")]
        public async Task<IActionResult> StartOrder(Guid orderId)
        {
            var result = await _orderService.StartOrderAsync(orderId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Tạo đơn hàng với thanh toán cọc qua ví hoặc gateway (NEW FLOW)
        /// </summary>
        [HttpPost("create-with-wallet")]
        public async Task<IActionResult> CreateOrderWithWallet([FromBody] CreateOrderWithWalletDTO request)
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return Unauthorized(new { Message = "Invalid authentication token" });
            }

            var result = await _orderService.CreateOrderWithWalletDepositAsync(customerId, request);

            return result.StatusCode switch
            {
                201 => CreatedAtAction(nameof(GetOrderById), new { orderId = ((dynamic)result.Data).OrderId }, result),
                404 => NotFound(result),
                400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Xác thực mã đơn hàng 6 ký tự (Dành cho nhân viên khi khách đến nhận xe)
        /// </summary>
        [HttpPost("verify-code")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> VerifyOrderCode([FromBody] VerifyOrderCodeDTO request)
        {
            var result = await _orderService.VerifyOrderCodeAsync(request.OrderCode);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        /// <summary>
        /// Xác nhận đơn hàng và chuyển sang trạng thái ONGOING (Nhân viên xác nhận sau khi kiểm tra)
        /// </summary>
        [HttpPut("{orderId}/confirm")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> ConfirmOrderByStaff(Guid orderId)
        {
            var staffIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (staffIdClaim == null || !Guid.TryParse(staffIdClaim.Value, out var staffId))
            {
                return Unauthorized(new { Message = "Invalid authentication token" });
            }

            var result = await _orderService.ConfirmOrderByStaffAsync(orderId, staffId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                404 => NotFound(result),
                400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
