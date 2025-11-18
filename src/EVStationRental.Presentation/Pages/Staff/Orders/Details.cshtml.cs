using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using EVStationRental.Services.InternalServices.IServices.IAccountServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.InternalServices.IServices.IPaymentServices;
using EVStationRental.Services.InternalServices.IServices.IDamageReportServices;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.DTOs;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.DTOs.PaymentDTOs;
using EVStationRental.Common.DTOs.DamageReportDTOs;
using EVStationRental.Common.Enums.EnumModel;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
    public bool IsPaymentCompleted { get; set; }
    public bool IsBatteryUpdateCompleted { get; set; }
    public bool IsDamageReportCreated { get; set; }
    public ViewDamageReportResponseDTO? DamageReport { get; set; }
    public List<SelectListItem> DamageLevels { get; set; } = new();

    [BindProperty]
    public int NewBatteryLevel { get; set; }

    [BindProperty]
    public CreateDamageReportRequestDTO CreateDamageReportDto { get; set; } = new();

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

        // Check if damage report was just created (from TempData)
        IsDamageReportCreated = TempData["DamageReportCreated"] as bool? ?? false;

        // Load damage report if exists
        try
        {
            var damageReportResult = await _damageReportService.GetDamageReportByOrderIdAsync(order.OrderId);

            _logger.LogInformation("Damage report query result: StatusCode={StatusCode}, HasData={HasData}",
                damageReportResult.StatusCode,
                damageReportResult.Data != null);

            if (damageReportResult.StatusCode == 200 && damageReportResult.Data != null)
            {
                if (damageReportResult.Data is ViewDamageReportResponseDTO damageReport)
                {
                    DamageReport = damageReport;
                    _logger.LogInformation("Damage report loaded successfully for Order {OrderId}, DamageId={DamageId}",
                        order.OrderId, damageReport.DamageId);
                }
                else
                {
                    _logger.LogWarning("Failed to cast damage report data for Order {OrderId}. Data type: {DataType}",
                        order.OrderId, damageReportResult.Data.GetType().Name);
                }
            }
            else if (damageReportResult.StatusCode == 404)
            {
                _logger.LogInformation("No damage report found for Order {OrderId} (expected for new orders)", order.OrderId);
            }
            else
            {
                _logger.LogWarning("Failed to load damage report for Order {OrderId}: {Message}",
                    order.OrderId, damageReportResult.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading damage report for Order {OrderId}", order.OrderId);
        }

        // Load damage levels for dropdown
        DamageLevels = Enum.GetValues(typeof(DamageLevelEnum))
            .Cast<DamageLevelEnum>()
            .Select(d => new SelectListItem
            {
                Value = d.ToString(),
                Text = d.ToString()
            }).ToList();

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
        try
        {
            // Load order first to get CustomerId and calculate final price
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
                _logger.LogInformation("Final price calculated: {FinalPrice} for Order {OrderId}", finalPrice, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating final price for order {OrderId}", id);
                TempData["Err"] = $"Không thể tính toán số tiền thanh toán. Lỗi: {ex.Message}";
                return RedirectToPage(new { id });
            }

            // IMPORTANT: Process payment FIRST before updating return time
            // Because UpdateReturnTime might change order status to COMPLETED
            var paymentDto = new FinalizeReturnPaymentDTO
            {
                AccountId = order.CustomerId,
                Amount = finalPrice,
                FinalPaymentMethod = "WALLET"
            };

            _logger.LogInformation("Processing payment for Order {OrderId}, Customer {CustomerId}, Amount {Amount}",
                id, order.CustomerId, finalPrice);

            var paymentResult = await _paymentService.FinalizeReturnPaymentAsync(paymentDto);

            if (paymentResult.StatusCode != 200)
            {
                _logger.LogError("Payment failed for Order {OrderId}: {Message}", id, paymentResult.Message);
                TempData["Err"] = $"Thanh toán thất bại: {paymentResult.Message}. Vui lòng kiểm tra số dư ví khách hàng.";
                return RedirectToPage(new { id });
            }

            _logger.LogInformation("Payment successful for Order {OrderId}, now updating return time", id);

            // After successful payment, update return time
            var updateReturnTimeResult = await _orderService.UpdateReturnTimeAsync(id);
            if (updateReturnTimeResult.StatusCode != 200)
            {
                _logger.LogWarning("Payment successful but UpdateReturnTime failed for Order {OrderId}: {Message}",
                    id, updateReturnTimeResult.Message);
                TempData["Ok"] = $"Đã thanh toán thành công {finalPrice.ToString("N0")} đ";
                // TempData["Err"] = $"Cảnh báo: Không thể cập nhật thời gian trả xe. {updateReturnTimeResult.Message}";
            }
            else
            {
                _logger.LogInformation("Successfully completed order {OrderId}: payment and return time updated", id);
                TempData["Ok"] = $"Đã thanh toán thành công {finalPrice.ToString("N0")} đ và cập nhật thời gian trả xe";
            }

            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in OnPostUpdateReturnTimeAsync for order {OrderId}", id);
            TempData["Err"] = $"Lỗi không mong muốn: {ex.Message}";
            return RedirectToPage(new { id });
        }
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

    public async Task<IActionResult> OnPostCreateDamageReportAsync(Guid id)
    {
        // Load order first to get VehicleId
        var orderResult = await _orderService.GetOrderByIdAsync(id);
        if (orderResult.StatusCode != 200 || orderResult.Data is not ViewOrderResponseDTO order)
        {
            TempData["Err"] = "Không tìm thấy đơn hàng";
            return RedirectToPage(new { id });
        }

        // Set OrderId and VehicleId
        CreateDamageReportDto.OrderId = id;
        CreateDamageReportDto.VehicleId = order.VehicleId;

        // Validate input
        if (string.IsNullOrWhiteSpace(CreateDamageReportDto.Description))
        {
            TempData["Err"] = "Vui lòng nhập mô tả hư hỏng";
            return RedirectToPage(new { id });
        }

        if (CreateDamageReportDto.EstimatedCost < 0)
        {
            TempData["Err"] = "Chi phí ước tính không hợp lệ";
            return RedirectToPage(new { id });
        }

        var result = await _damageReportService.CreateDamageReportAsync(CreateDamageReportDto);

        if (result.StatusCode == 200)
        {
            _logger.LogInformation("Damage report created successfully for Order {OrderId}", id);
            TempData["Ok"] = "Đã tạo báo cáo hư hỏng thành công";
            TempData["DamageReportCreated"] = true; // Flag to prevent form from showing again
        }
        else
        {
            _logger.LogError("Failed to create damage report for Order {OrderId}: {Message}", id, result.Message);
            TempData["Err"] = result.Message ?? "Không thể tạo báo cáo hư hỏng";
        }

        // Clear form data to prevent resubmission
        ModelState.Clear();
        CreateDamageReportDto = new CreateDamageReportRequestDTO();

        return RedirectToPage(new { id });
    }
}
