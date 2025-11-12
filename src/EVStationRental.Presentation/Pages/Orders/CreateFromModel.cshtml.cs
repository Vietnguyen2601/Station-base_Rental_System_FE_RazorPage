using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.DTOs.VehicleModelDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Orders;

[Authorize(Roles = "Customer")]
public class CreateFromModelModel : PageModel
{
    private const string OrderCreateMethod = nameof(IOrderService.CreateOrderWithWalletDepositAsync);

    private readonly IVehicleService _vehicleService;
    private readonly IVehicleModelService _vehicleModelService;
    private readonly IOrderService _orderService;
    private readonly ILogger<CreateFromModelModel> _logger;

    public CreateFromModelModel(IVehicleService vehicleService, IVehicleModelService vehicleModelService, IOrderService orderService, ILogger<CreateFromModelModel> logger)
    {
        _vehicleService = vehicleService;
        _vehicleModelService = vehicleModelService;
        _orderService = orderService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public Guid StationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ModelId { get; set; }

    [BindProperty]
    public BookingInput Input { get; set; } = new();

    public decimal? EstimatedPrice { get; private set; }
    public bool SlotAvailable { get; private set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (StationId == Guid.Empty || ModelId == Guid.Empty)
        {
            return RedirectToPage("/Stations/Browse");
        }

        Input.StartTime = DateTime.Now.AddHours(2);
        Input.EndTime = DateTime.Now.AddHours(6);
        Input.PaymentMethod ??= "WALLET";

        return await RefreshAsync(cancellationToken);
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostPreviewAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RefreshAsync(cancellationToken);
        }

        return await RefreshAsync(cancellationToken);
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostConfirmAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RefreshAsync(cancellationToken);
        }

        if (Input.StartTime <= DateTime.Now)
        {
            ModelState.AddModelError(nameof(Input.StartTime), "Bắt đầu phải sau hiện tại");
            return await RefreshAsync(cancellationToken);
        }

        if (Input.EndTime <= Input.StartTime)
        {
            ModelState.AddModelError(nameof(Input.EndTime), "Kết thúc phải sau bắt đầu");
            return await RefreshAsync(cancellationToken);
        }

        var candidate = await FindVehicleCandidateAsync(cancellationToken);
        if (candidate is null)
        {
            TempData["Err"] = "Không còn xe trống cho khoảng thời gian này.";
            return await RefreshAsync(cancellationToken);
        }

        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            TempData["Err"] = "Phiên đăng nhập không hợp lệ.";
            return RedirectToPage("/Auth/Login");
        }

        var result = await InvokeOrderCreationAsync(customerId, candidate.VehicleId);
        if (result == null || result.StatusCode < 200 || result.StatusCode >= 300)
        {
            TempData["Err"] = result?.Message ?? "Tạo đơn thất bại. Vui lòng thử lại.";
            return await RefreshAsync(cancellationToken);
        }

        TempData["Ok"] = result.Message ?? "Đặt xe thành công.";
        return RedirectToPage("/Customer/Dashboard");
    }

    private async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        if (Input.StartTime == default)
        {
            Input.StartTime = DateTime.Now.AddHours(2);
        }

        if (Input.EndTime == default || Input.EndTime <= Input.StartTime)
        {
            Input.EndTime = Input.StartTime.AddHours(4);
        }

        Input.PaymentMethod ??= "WALLET";

        var candidate = await FindVehicleCandidateAsync(cancellationToken);
        SlotAvailable = candidate is not null;
        var pricePerHour = candidate?.PricePerHour ?? await GetModelPricePerHourAsync(cancellationToken);
        EstimatedPrice = pricePerHour > 0
            ? CalculateTieredPrice((decimal)(Input.EndTime - Input.StartTime).TotalHours, pricePerHour)
            : null;

        return Page();
    }

    private async Task<ViewVehicleResponse?> FindVehicleCandidateAsync(CancellationToken cancellationToken)
    {
        if (StationId == Guid.Empty || ModelId == Guid.Empty)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var response = await _vehicleService.GetVehicleWithHighestBatteryByModelAndStationAsync(ModelId, StationId);
        if (response == null)
        {
            return null;
        }

        if (response.StatusCode < 200 || response.StatusCode >= 300)
        {
            _logger.LogWarning("Không lấy được xe cho model {ModelId} tại trạm {StationId}: {Message}", ModelId, StationId, response.Message);
            return null;
        }

        if (response.Data is not ViewVehicleResponse vehicle)
        {
            return null;
        }

        if (!string.Equals(vehicle.Status, VehicleStatus.AVAILABLE.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Xe {VehicleId} không ở trạng thái AVAILABLE", vehicle.VehicleId);
            return null;
        }

        _logger.LogInformation("Chọn xe {VehicleId} tại trạm {StationId} cho mẫu {ModelId}", vehicle.VehicleId, StationId, ModelId);
        return vehicle;
    }

    private async Task<decimal> GetModelPricePerHourAsync(CancellationToken cancellationToken)
    {
        if (ModelId == Guid.Empty)
        {
            return 0;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = await _vehicleModelService.GetVehicleModelByIdAsync(ModelId);
        if (result?.Data is ViewVehicleModelResponseDTO dto)
        {
            return dto.PricePerHour;
        }

        return 0;
    }

    private static decimal CalculateTieredPrice(decimal totalHours, decimal pricePerHour)
    {
        if (totalHours <= 0 || pricePerHour <= 0)
        {
            return 0;
        }

        decimal totalPrice = 0;
        decimal remainingHours = totalHours;
        int tierLevel = 0;

        while (remainingHours > 0)
        {
            var hoursInTier = Math.Min(remainingHours, 12);
            var discountMultiplier = 1 - (tierLevel * 0.05m);
            discountMultiplier = Math.Max(discountMultiplier, 0.85m);

            totalPrice += hoursInTier * pricePerHour * discountMultiplier;

            remainingHours -= hoursInTier;
            tierLevel++;
        }

        return Math.Round(totalPrice, 0);
    }

    private Task<IServiceResult> InvokeOrderCreationAsync(Guid customerId, Guid vehicleId)
    {
        return OrderCreateMethod switch
        {
            nameof(IOrderService.CreateOrderWithWalletDepositAsync) =>
                _orderService.CreateOrderWithWalletDepositAsync(customerId, new CreateOrderWithWalletDTO
                {
                    VehicleId = vehicleId,
                    StartTime = Input.StartTime,
                    EndTime = Input.EndTime,
                    PromotionCode = Input.PromotionCode,
                    PaymentMethod = Input.PaymentMethod ?? "WALLET"
                }),
            nameof(IOrderService.CreateOrderAsync) =>
                _orderService.CreateOrderAsync(customerId, new CreateOrderRequestDTO
                {
                    VehicleId = vehicleId,
                    StartTime = Input.StartTime,
                    EndTime = Input.EndTime,
                    PromotionCode = Input.PromotionCode
                }),
            _ => throw new NotSupportedException($"Hàm tạo đơn '{OrderCreateMethod}' chưa được hỗ trợ.")
        };
    }

    public sealed class BookingInput
    {
        [Required]
        [Display(Name = "Bắt đầu")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "Kết thúc")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Mã khuyến mãi")]
        public string? PromotionCode { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public string? PaymentMethod { get; set; }
    }
}
