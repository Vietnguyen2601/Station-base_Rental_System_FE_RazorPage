using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.DTOs.VehicleModelDTOs;
using EVStationRental.Common.DTOs.WalletDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
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
    private readonly IWalletService _walletService;
    private readonly ILogger<CreateFromModelModel> _logger;

    private static readonly IReadOnlyList<DiscountTierInfo> _tierInfos = new[]
    {
        new DiscountTierInfo("0 - 12 giờ", "Giá tiêu chuẩn", 0),
        new DiscountTierInfo("12 - 24 giờ", "Giảm 5% cho phần thời gian vượt 12h", 5),
        new DiscountTierInfo("24 - 36 giờ", "Giảm 10% cho phần vượt 24h", 10),
        new DiscountTierInfo("36 - 48 giờ", "Giảm 15% cho phần vượt 36h", 15),
        new DiscountTierInfo("Trên 48 giờ", "Giữ mức giảm tối đa 15%", 15)
    };

    public CreateFromModelModel(
        IVehicleService vehicleService,
        IVehicleModelService vehicleModelService,
        IOrderService orderService,
        IWalletService walletService,
        ILogger<CreateFromModelModel> logger)
    {
        _vehicleService = vehicleService;
        _vehicleModelService = vehicleModelService;
        _orderService = orderService;
        _walletService = walletService;
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
    public decimal? DepositAmount { get; private set; }
    public decimal? EffectiveDiscountPercent { get; private set; }
    public string DiscountMessage { get; private set; } = string.Empty;
    public decimal RentalHours { get; private set; }
    public IReadOnlyList<DiscountTierInfo> DiscountTiers => _tierInfos;
    public int ActiveDiscountTier { get; private set; }
    public decimal PricePerHour { get; private set; }
    public string MinStartIso => GetIsoString(GetMinimumStartTime());
    public DateTime MinStartTimeDisplay => GetMinimumStartTime();
    public string CurrentStartIso => GetIsoString(Input.StartTime);
    public bool IsSameDayBlocked => Input.StartTime.Date <= DateTime.Today;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (StationId == Guid.Empty || ModelId == Guid.Empty)
        {
            return RedirectToPage("/Stations/Browse");
        }

        var defaultStart = GetMinimumStartTime().AddHours(8);
        Input.StartTime = defaultStart;
        Input.EndTime = defaultStart.AddHours(4);
        Input.PaymentMethod = "WALLET";

        return await RefreshAsync(cancellationToken);
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostPreviewAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RefreshAsync(cancellationToken);
        }

        if (!ValidateAdvanceBookingWindow())
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

        Input.PaymentMethod = "WALLET";

        if (!ValidateAdvanceBookingWindow())
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

        if (!Input.AcceptDepositPolicy)
        {
            ModelState.AddModelError(nameof(Input.AcceptDepositPolicy), "Vui lòng xác nhận quy định đặt cọc 10%.");
        }

        if (!Input.AcceptUsagePolicy)
        {
            ModelState.AddModelError(nameof(Input.AcceptUsagePolicy), "Bạn cần cam kết tuân thủ quy định sử dụng xe.");
        }

        if (!ModelState.IsValid)
        {
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

        var hoursForQuote = (decimal)(Input.EndTime - Input.StartTime).TotalHours;
        var rateForQuote = candidate.PricePerHour > 0 ? candidate.PricePerHour : await GetModelPricePerHourAsync(cancellationToken);
        var quoteForValidation = CalculatePriceQuote(hoursForQuote, rateForQuote);
        var depositNeeded = Math.Round(quoteForValidation.TotalPrice * 0.10m, 0);

        if (!await EnsureWalletBalanceAsync(customerId, depositNeeded))
        {
            return await RefreshAsync(cancellationToken);
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
            var fallbackStart = GetMinimumStartTime().AddHours(8);
            Input.StartTime = fallbackStart;
        }

        if (Input.EndTime == default || Input.EndTime <= Input.StartTime)
        {
            Input.EndTime = Input.StartTime.AddHours(4);
        }

        Input.PaymentMethod = "WALLET";

        var candidate = await FindVehicleCandidateAsync(cancellationToken);
        SlotAvailable = candidate is not null;
        var pricePerHour = candidate?.PricePerHour ?? await GetModelPricePerHourAsync(cancellationToken);
        PricePerHour = pricePerHour;
        var totalHours = (decimal)(Input.EndTime - Input.StartTime).TotalHours;
        RentalHours = totalHours > 0 ? Math.Round(totalHours, 2) : 0;

        if (pricePerHour > 0 && totalHours > 0)
        {
            var quote = CalculatePriceQuote(totalHours, pricePerHour);
            EstimatedPrice = quote.TotalPrice;
            EffectiveDiscountPercent = quote.EffectiveDiscountPercent * 100;
            DepositAmount = Math.Round(quote.TotalPrice * 0.10m, 0);
            ActiveDiscountTier = quote.TierLevel;
            DiscountMessage = quote.TierLevel switch
            {
                4 => "Giữ mức giảm tối đa 15% cho phần vượt 48 giờ.",
                3 => "Đang áp dụng tối đa 15% cho phần thời gian vượt 36 giờ.",
                2 => "Đang áp dụng giảm 10% cho phần thời gian sau 24 giờ.",
                1 => "Đang áp dụng giảm 5% cho phần thời gian sau 12 giờ.",
                _ => "Giá tiêu chuẩn cho 12 giờ đầu tiên."
            };
        }
        else
        {
            EstimatedPrice = null;
            DepositAmount = null;
            EffectiveDiscountPercent = null;
            PricePerHour = pricePerHour;
            ActiveDiscountTier = 0;
            DiscountMessage = "Nhập thời gian để xem ưu đãi.";
        }

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

    private static decimal GetDiscountMultiplier(decimal totalHours)
    {
        if (totalHours <= 0) return 0;
        if (totalHours <= 12) return 1m;
        if (totalHours <= 24) return 0.95m;
        if (totalHours <= 36) return 0.90m;
        return 0.85m;
    }

    private static int DetermineTierIndex(decimal totalHours)
    {
        if (totalHours <= 12) return 0;
        if (totalHours <= 24) return 1;
        if (totalHours <= 36) return 2;
        if (totalHours <= 48) return 3;
        return 4;
    }

    private static PriceQuote CalculatePriceQuote(decimal totalHours, decimal pricePerHour)
    {
        if (totalHours <= 0 || pricePerHour <= 0)
        {
            return new PriceQuote(0, 0, 0);
        }

        var multiplier = GetDiscountMultiplier(totalHours);
        var totalPrice = Math.Round(totalHours * pricePerHour * multiplier, 0);
        var effectiveDiscount = 1 - multiplier;
        var tierIndex = DetermineTierIndex(totalHours);

        return new PriceQuote(totalPrice, Math.Round(effectiveDiscount, 4), tierIndex);
    }

    private bool ValidateAdvanceBookingWindow()
    {
        if (Input.StartTime.Date <= DateTime.Today)
        {
            ModelState.AddModelError(nameof(Input.StartTime), "Hệ thống chỉ nhận đặt xe từ ngày mai trở đi.");
            return false;
        }

        return true;
    }

    private static string GetIsoString(DateTime value) => value.ToString("yyyy-MM-ddTHH:mm");

    private DateTime GetMinimumStartTime() => DateTime.Today.AddDays(1);

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

    private async Task<bool> EnsureWalletBalanceAsync(Guid customerId, decimal requiredDeposit)
    {
        if (requiredDeposit <= 0)
        {
            return true;
        }

        var walletResult = await _walletService.GetWalletBalanceAsync(customerId);
        if (walletResult?.Data is WalletBalanceDTO walletDto)
        {
            if (walletDto.Balance >= requiredDeposit)
            {
                return true;
            }

            ModelState.AddModelError(string.Empty, $"Số dư ví hiện tại ({walletDto.Balance:n0} ₫) không đủ để đặt cọc {requiredDeposit:n0} ₫.");
            return false;
        }

        ModelState.AddModelError(string.Empty, walletResult?.Message ?? "Không thể kiểm tra số dư ví.");
        return false;
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
        public string? PaymentMethod { get; set; } = "WALLET";

        [Display(Name = "Tôi hiểu đặt cọc 10% là bắt buộc")]
        public bool AcceptDepositPolicy { get; set; }

        [Display(Name = "Tôi cam kết tuân thủ quy định sử dụng xe & thời gian hoàn trả")]
        public bool AcceptUsagePolicy { get; set; }
    }

    public sealed record DiscountTierInfo(string Range, string Description, int Percent);

    private sealed record PriceQuote(decimal TotalPrice, decimal EffectiveDiscountPercent, int TierLevel);
}
