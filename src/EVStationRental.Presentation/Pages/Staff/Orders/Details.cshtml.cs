using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using EVStationRental.Services.InternalServices.IServices.IAccountServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.InternalServices.IServices.IPaymentServices;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.DTOs;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.DTOs.PaymentDTOs;
using System;
using System.Threading.Tasks;

namespace EVStationRental.Presentation.Pages.Staff.Orders;

[Authorize(Roles = "Staff")]
public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IAccountService _accountService;
    private readonly IVehicleService _vehicleService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IOrderService orderService,
        IAccountService accountService,
        IVehicleService vehicleService,
        IPaymentService paymentService,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _accountService = accountService;
        _vehicleService = vehicleService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public ViewOrderResponseDTO? Order { get; set; }
    public ViewAccountDTO? Customer { get; set; }
    public ViewVehicleResponse? Vehicle { get; set; }
    public ViewAccountDTO? Staff { get; set; }
    public decimal FinalPrice { get; set; }
    public bool IsPaymentCompleted { get; set; }
    public bool IsBatteryUpdateCompleted { get; set; }

    [BindProperty]
    public int NewBatteryLevel { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        // Load order
        var orderResult = await _orderService.GetOrderByIdAsync(id);
        if (orderResult.StatusCode != 200 || orderResult.Data is not ViewOrderResponseDTO order)
        {
            TempData["Err"] = orderResult.Message ?? "Không tìm thấy đơn hàng";
            return RedirectToPage("/Staff/Orders/Index");
        }

        Order = order;

        // Load customer info
        var customerResult = await _accountService.GetAccountByIdAsync(order.CustomerId);
        if (customerResult.StatusCode == 200 && customerResult.Data is ViewAccountDTO customer)
        {
            Customer = customer;
        }

        // Load vehicle info
        var vehicleResult = await _vehicleService.GetVehicleByIdAsync(order.VehicleId);
        if (vehicleResult.StatusCode == 200 && vehicleResult.Data is ViewVehicleResponse vehicle)
        {
            Vehicle = vehicle;
        }

        // Load staff info if exists
        if (order.StaffId.HasValue)
        {
            var staffResult = await _accountService.GetAccountByIdAsync(order.StaffId.Value);
            if (staffResult.StatusCode == 200 && staffResult.Data is ViewAccountDTO staff)
            {
                Staff = staff;
            }
        }

        // Calculate final price for ONGOING orders (even without ReturnTime) or orders with ReturnTime
        if (order.Status == Common.Enums.EnumModel.OrderStatus.ONGOING || order.ReturnTime.HasValue)
        {
            try
            {
                FinalPrice = await _paymentService.CalculateFinalPriceAsync(order.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating final price for order {OrderId}", order.OrderId);
                FinalPrice = 0;
            }
        }

        // Check if payment is completed (order is COMPLETED or REFUNDED)
        IsPaymentCompleted = order.Status == Common.Enums.EnumModel.OrderStatus.COMPLETED ||
                            order.Status == Common.Enums.EnumModel.OrderStatus.REFUNDED;

        // Check if battery update is completed (from TempData)
        IsBatteryUpdateCompleted = TempData["BatteryUpdateCompleted"] as bool? ?? false;

        return Page();
    }

    public async Task<IActionResult> OnPostStartOrderAsync(Guid id)
    {
        var result = await _orderService.StartOrderAsync(id);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = "Đã cập nhật trạng thái đơn hàng sang ONGOING";
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể cập nhật trạng thái đơn hàng";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateReturnTimeAsync(Guid id)
    {
        var result = await _orderService.UpdateReturnTimeAsync(id);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = "Đã cập nhật thời gian trả xe";
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể cập nhật thời gian trả xe";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateBatteryAsync(Guid id)
    {
        // Load order first to get VehicleId and status
        var orderResult = await _orderService.GetOrderByIdAsync(id);
        if (orderResult.StatusCode != 200 || orderResult.Data is not ViewOrderResponseDTO order)
        {
            TempData["Err"] = "Không tìm thấy đơn hàng";
            return RedirectToPage(new { id });
        }

        // Check if order is ONGOING
        if (order.Status != Common.Enums.EnumModel.OrderStatus.ONGOING)
        {
            TempData["Err"] = "Chỉ có thể cập nhật pin khi đơn hàng đang trong trạng thái ONGOING";
            return RedirectToPage(new { id });
        }

        // Validate battery level
        if (NewBatteryLevel < 0 || NewBatteryLevel > 100)
        {
            TempData["Err"] = "Mức pin phải từ 0 đến 100";
            return RedirectToPage(new { id });
        }

        var updateDto = new UpdateVehicleRequestDTO
        {
            BatteryLevel = NewBatteryLevel
        };

        var result = await _vehicleService.UpdateVehicleAsync(order.VehicleId, updateDto);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = $"Đã cập nhật mức pin xe thành {NewBatteryLevel}%";
            TempData["BatteryUpdateCompleted"] = true;
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể cập nhật mức pin";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostFinalizePaymentAsync(Guid id)
    {
        try
        {
            // Load order first to get CustomerId
            var orderResult = await _orderService.GetOrderByIdAsync(id);
            if (orderResult.StatusCode != 200 || orderResult.Data is not ViewOrderResponseDTO order)
            {
                _logger.LogWarning("Order not found. OrderId: {OrderId}, StatusCode: {StatusCode}", id, orderResult.StatusCode);
                TempData["Err"] = $"Không tìm thấy đơn hàng. Lỗi: {orderResult.Message}";
                return RedirectToPage(new { id });
            }

            _logger.LogInformation("Processing payment for Order {OrderId}, Customer {CustomerId}", order.OrderId, order.CustomerId);

            // Calculate final price
            decimal finalPrice;
            try
            {
                finalPrice = await _paymentService.CalculateFinalPriceAsync(id);
                _logger.LogInformation("Final price calculated: {FinalPrice} for Order {OrderId}", finalPrice, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating final price for order {OrderId}", id);
                TempData["Err"] = $"Không thể tính toán số tiền thanh toán. Lỗi: {ex.Message}";
                return RedirectToPage(new { id });
            }

            if (finalPrice <= 0)
            {
                TempData["Err"] = $"Số tiền thanh toán không hợp lệ: {finalPrice}";
                return RedirectToPage(new { id });
            }

            // Try using CompleteOrderWithFinalPaymentAsync for stored procedure approach
            _logger.LogInformation("Calling CompleteOrderWithFinalPaymentAsync for Order {OrderId} with WALLET payment", id);

            var result = await _paymentService.CompleteOrderWithFinalPaymentAsync(id, "WALLET");

            _logger.LogInformation("Payment result: StatusCode={StatusCode}, Message={Message}",
                result.StatusCode, result.Message);

            if (result.StatusCode == 200)
            {
                // After successful payment, update return time
                var updateReturnTimeResult = await _orderService.UpdateReturnTimeAsync(id);

                if (updateReturnTimeResult.StatusCode == 200)
                {
                    TempData["Ok"] = $"Đã thanh toán thành công {finalPrice.ToString("N0")} đ và cập nhật thời gian trả xe";
                    TempData["PaymentCompleted"] = true;
                }
                else
                {
                    _logger.LogWarning("Payment successful but UpdateReturnTime failed for Order {OrderId}: {Message}",
                        id, updateReturnTimeResult.Message);
                    TempData["Ok"] = $"Đã thanh toán thành công {finalPrice.ToString("N0")} đ";
                    TempData["Err"] = $"Cảnh báo: Không thể cập nhật thời gian trả xe. Lỗi: {updateReturnTimeResult.Message}";
                }
            }
            else
            {
                // If CompleteOrderWithFinalPaymentAsync fails, try FinalizeReturnPaymentAsync as fallback
                _logger.LogWarning("CompleteOrderWithFinalPaymentAsync failed, trying FinalizeReturnPaymentAsync as fallback");

                var paymentDto = new FinalizeReturnPaymentDTO
                {
                    AccountId = order.CustomerId,
                    Amount = finalPrice,
                    FinalPaymentMethod = "WALLET"
                };

                var fallbackResult = await _paymentService.FinalizeReturnPaymentAsync(paymentDto);

                if (fallbackResult.StatusCode == 200)
                {
                    // After successful payment, update return time
                    var updateReturnTimeResult = await _orderService.UpdateReturnTimeAsync(id);

                    if (updateReturnTimeResult.StatusCode == 200)
                    {
                        TempData["Ok"] = $"Đã thanh toán thành công {finalPrice.ToString("N0")} đ và cập nhật thời gian trả xe";
                        TempData["PaymentCompleted"] = true;
                    }
                    else
                    {
                        _logger.LogWarning("Payment successful but UpdateReturnTime failed for Order {OrderId}: {Message}",
                            id, updateReturnTimeResult.Message);
                        TempData["Ok"] = $"Đã thanh toán thành công {finalPrice.ToString("N0")} đ";
                        TempData["Err"] = $"Cảnh báo: Không thể cập nhật thời gian trả xe. Lỗi: {updateReturnTimeResult.Message}";
                    }
                }
                else
                {
                    TempData["Err"] = $"Không thể hoàn tất thanh toán. Lỗi: {fallbackResult.Message}";
                }
            }

            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in OnPostFinalizePaymentAsync for order {OrderId}", id);
            TempData["Err"] = $"Lỗi không mong muốn: {ex.Message}";
            return RedirectToPage(new { id });
        }
    }
}
