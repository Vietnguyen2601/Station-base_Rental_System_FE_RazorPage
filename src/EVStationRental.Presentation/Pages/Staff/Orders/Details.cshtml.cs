using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using EVStationRental.Services.InternalServices.IServices.IAccountServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.InternalServices.IServices.IPaymentServices;
using EVStationRental.Services.InternalServices.IServices.IDamageReportServices;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.DTOs;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.DTOs.DamageReportDTOs;
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
    private readonly IDamageReportService _damageReportService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IOrderService orderService,
        IAccountService accountService,
        IVehicleService vehicleService,
        IPaymentService paymentService,
        IDamageReportService damageReportService,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _accountService = accountService;
        _vehicleService = vehicleService;
        _paymentService = paymentService;
        _damageReportService = damageReportService;
        _logger = logger;
    }

    public ViewOrderResponseDTO? Order { get; set; }
    public ViewAccountDTO? Customer { get; set; }
    public ViewVehicleResponse? Vehicle { get; set; }
    public ViewAccountDTO? Staff { get; set; }
    public decimal FinalPrice { get; set; }

    [BindProperty]
    public string DamageDescription { get; set; } = string.Empty;

    [BindProperty]
    public decimal EstimatedCost { get; set; }

    [BindProperty]
    public string? DamageImg { get; set; }

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

        // Calculate final price if order has return time
        if (order.ReturnTime.HasValue)
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

    public async Task<IActionResult> OnPostCreateDamageReportAsync(Guid id)
    {
        // Load order first to get VehicleId
        var orderResult = await _orderService.GetOrderByIdAsync(id);
        if (orderResult.StatusCode != 200 || orderResult.Data is not ViewOrderResponseDTO order)
        {
            TempData["Err"] = "Không tìm thấy đơn hàng";
            return RedirectToPage(new { id });
        }

        // Validate input
        if (string.IsNullOrWhiteSpace(DamageDescription))
        {
            TempData["Err"] = "Vui lòng nhập mô tả hư hỏng";
            return RedirectToPage(new { id });
        }

        if (EstimatedCost < 0)
        {
            TempData["Err"] = "Chi phí ước tính không hợp lệ";
            return RedirectToPage(new { id });
        }

        var damageReportDto = new CreateDamageReportRequestDTO
        {
            OrderId = id,
            VehicleId = order.VehicleId,
            Description = DamageDescription,
            EstimatedCost = EstimatedCost,
            Img = DamageImg
        };

        var result = await _damageReportService.CreateDamageReportAsync(damageReportDto);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = "Đã tạo báo cáo hư hỏng thành công";
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể tạo báo cáo hư hỏng";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostFinalizePaymentAsync(Guid id)
    {
        // Load order first to get CustomerId
        var orderResult = await _orderService.GetOrderByIdAsync(id);
        if (orderResult.StatusCode != 200 || orderResult.Data is not ViewOrderResponseDTO order)
        {
            TempData["Err"] = "Không tìm thấy đơn hàng";
            return RedirectToPage(new { id });
        }

        // Calculate final price
        decimal finalPrice;
        try
        {
            finalPrice = await _paymentService.CalculateFinalPriceAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating final price for order {OrderId}", id);
            TempData["Err"] = "Không thể tính toán số tiền thanh toán";
            return RedirectToPage(new { id });
        }

        if (finalPrice <= 0)
        {
            TempData["Err"] = "Số tiền thanh toán không hợp lệ";
            return RedirectToPage(new { id });
        }

        var paymentDto = new FinalizeReturnPaymentDTO
        {
            AccountId = order.CustomerId,
            Amount = finalPrice,
            FinalPaymentMethod = "WALLET"
        };

        var result = await _paymentService.FinalizeReturnPaymentAsync(paymentDto);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = $"Đã thanh toán thành công {finalPrice.ToString("N0")} đ";
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể hoàn tất thanh toán";
        }

        return RedirectToPage(new { id });
    }
}
