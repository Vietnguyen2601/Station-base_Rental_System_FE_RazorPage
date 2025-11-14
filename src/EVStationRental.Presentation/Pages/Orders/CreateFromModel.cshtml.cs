using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.DTOs.StationDTOs;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.DTOs.WalletDTOs;
using EVStationRental.Presentation.Models.Orders;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using EVStationRental.Services.InternalServices.IServices.IStationServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace EVStationRental.Presentation.Pages.Orders;

[Authorize(Roles = "Customer")]
public class CreateFromModelModel : PageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IStationService _stationService;
    private readonly IOrderService _orderService;
    private readonly IWalletService _walletService;
    private readonly ILogger<CreateFromModelModel> _logger;

    public CreateFromModelModel(
        IVehicleService vehicleService,
        IStationService stationService,
        IOrderService orderService,
        IWalletService walletService,
        ILogger<CreateFromModelModel> logger)
    {
        _vehicleService = vehicleService;
        _stationService = stationService;
        _orderService = orderService;
        _walletService = walletService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public Guid StationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ModelId { get; set; }

    [BindProperty]
    public OrderCreateSummaryVm Order { get; set; } = new();

    public decimal WalletBalance { get; private set; }
    public bool HasEnoughDeposit { get; private set; }
    public bool SlotAvailable { get; private set; }
    public string? VehicleImageUrl { get; private set; }
    public string VehicleSpecs { get; private set; } = string.Empty;
    public IReadOnlyList<DiscountTierDisplay> DiscountTiers => _discountTiers;
    public int ActiveTierIndex { get; private set; }

    private static readonly DiscountTierDisplay[] _discountTiers =
    {
        new("0 - 12h", "Giá tiêu chuẩn", 0),
        new("12 - 24h", "Giảm 5% cho phần vượt 12h", 5),
        new("24 - 36h", "Giảm 10% cho phần vượt 24h", 10),
        new("36 - 48h", "Giảm 15% cho phần vượt 36h", 15),
        new("Trên 48h", "Giữ mức giảm tối đa 15%", 15)
    };

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (StationId == Guid.Empty || ModelId == Guid.Empty)
        {
            return RedirectToPage("/Stations/Browse");
        }

        Order.StationId = StationId;
        EnsureDefaultTimes();

        var loaded = await LoadOrderContextAsync(cancellationToken);
        if (!loaded)
        {
            return RedirectToPage("/Stations/Browse");
        }

        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostPreviewAsync(CancellationToken cancellationToken)
    {
        EnsureDefaultTimes();
        var loaded = await LoadOrderContextAsync(cancellationToken);
        if (!loaded)
        {
            return RedirectToPage("/Stations/Browse");
        }

        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostConfirmAsync(CancellationToken cancellationToken)
    {
        EnsureDefaultTimes();
        var loaded = await LoadOrderContextAsync(cancellationToken);
        if (!loaded)
        {
            return RedirectToPage("/Stations/Browse");
        }

        if (!Order.AcceptDepositPolicy)
        {
            ModelState.AddModelError("Order.AcceptDepositPolicy", "Bạn cần xác nhận quy định đặt cọc.");
        }

        if (!Order.AcceptUsagePolicy)
        {
            ModelState.AddModelError("Order.AcceptUsagePolicy", "Bạn cần cam kết tuân thủ quy định sử dụng xe.");
        }

        if (!SlotAvailable)
        {
            ModelState.AddModelError(string.Empty, "Hiện chưa có xe trống cho thời gian này.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!HasEnoughDeposit)
        {
            TempData["Error"] = "Số dư ví không đủ để đặt cọc 10%. Vui lòng nạp thêm.";
            return Page();
        }

        var customerId = GetCurrentUserId();
        if (customerId == Guid.Empty)
        {
            return Challenge();
        }

        var createResult = await _orderService.CreateOrderWithWalletDepositAsync(customerId, new CreateOrderWithWalletDTO
        {
            VehicleId = Order.VehicleId,
            StartTime = Order.StartTime,
            EndTime = Order.EndTime,
            PaymentMethod = Order.PaymentMethod,
            PromotionCode = Order.PromotionCode
        });

        if (createResult?.StatusCode is >= 200 and < 300 && createResult.Data is CreateOrderWithWalletResponseDTO dto)
        {
            return RedirectToPage("Confirm", new { id = dto.OrderId });
        }

        TempData["Error"] = createResult?.Message ?? "Không thể đặt xe. Vui lòng thử lại.";
        return Page();
    }

    private async Task<bool> LoadOrderContextAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Order.PaymentMethod = "WALLET";
        if (Order.StationId == Guid.Empty)
        {
            Order.StationId = StationId;
        }

        var vehicleResult = await _vehicleService.GetVehicleWithHighestBatteryByModelAndStationAsync(ModelId, Order.StationId);
        if (vehicleResult?.Data is ViewVehicleResponse vehicle)
        {
            SlotAvailable = true;
            Order.VehicleId = vehicle.VehicleId;
            Order.VehicleName = string.IsNullOrWhiteSpace(vehicle.ModelName)
                ? "Xe điện"
                : vehicle.ModelName;
            Order.StationName = vehicle.StationName ?? string.Empty;
            VehicleImageUrl = vehicle.Img;
            VehicleSpecs = vehicle.Specs ?? string.Empty;
        }
        else
        {
            SlotAvailable = false;
            Order.VehicleId = Guid.Empty;
            Order.VehicleName = "Chưa có xe phù hợp";
            Order.StationName = Order.StationName;
            VehicleImageUrl = null;
            VehicleSpecs = string.Empty;
            TempData["Error"] = vehicleResult?.Message ?? "Hiện chưa có xe trống cho khung giờ này.";
        }

        var stationResult = await _stationService.GetStationsByVehicleModelAsync(ModelId);
        if (stationResult?.Data is IEnumerable<StationWithAvailableVehiclesResponse> stations)
        {
            var stationInfo = stations.FirstOrDefault(s => s.StationId == Order.StationId);
            if (stationInfo != null)
            {
                if (string.IsNullOrWhiteSpace(Order.StationName))
                {
                    Order.StationName = stationInfo.Name;
                }
                Order.StationAddress = stationInfo.Address;
            }
        }

        Order.PromotionCode = string.IsNullOrWhiteSpace(Order.PromotionCode)
            ? null
            : Order.PromotionCode.Trim();

        var totalHours = (decimal)(Order.EndTime - Order.StartTime).TotalHours;
        ActiveTierIndex = DetermineTierIndex(totalHours);

        if (SlotAvailable && Order.VehicleId != Guid.Empty)
        {
            var priceResult = await _orderService.EstimateOrderPriceAsync(
                Order.VehicleId,
                Order.StartTime,
                Order.EndTime,
                Order.PromotionCode);

            if (priceResult?.Data is OrderPriceEstimateDTO priceDto)
            {
                Order.BasePrice = priceDto.BasePrice;
                Order.TotalAfterDiscount = priceDto.TotalPrice;
                Order.DiscountAmount = priceDto.DiscountAmount;
                Order.DepositAmount = priceDto.DepositAmount;
            }
            else
            {
                TempData["Error"] = priceResult?.Message ?? "Không thể tính chi phí tạm tính.";
                SlotAvailable = false;
                Order.BasePrice = Order.TotalAfterDiscount = Order.DiscountAmount = Order.DepositAmount = 0;
            }
        }
        else
        {
            Order.BasePrice = Order.TotalAfterDiscount = Order.DiscountAmount = Order.DepositAmount = 0;
        }

        var walletResult = await _walletService.GetWalletBalanceAsync(GetCurrentUserId());
        if (walletResult?.Data is WalletBalanceDTO walletDto)
        {
            WalletBalance = walletDto.Balance;
            HasEnoughDeposit = WalletBalance >= Order.DepositAmount;
        }
        else
        {
            WalletBalance = 0;
            HasEnoughDeposit = false;
        }

        return true;
    }

    private void EnsureDefaultTimes()
    {
        if (Order.StartTime == default || Order.StartTime <= DateTime.Now)
        {
            Order.StartTime = GetDefaultStartTime();
        }

        if (Order.EndTime == default || Order.EndTime <= Order.StartTime)
        {
            Order.EndTime = Order.StartTime.AddHours(4);
        }
    }

    private DateTime GetDefaultStartTime()
    {
        return DateTime.Now.Date.AddDays(1).AddHours(8);
    }

    private Guid GetCurrentUserId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : Guid.Empty;
    }

    private static int DetermineTierIndex(decimal hours)
    {
        if (hours <= 12) return 0;
        if (hours <= 24) return 1;
        if (hours <= 36) return 2;
        if (hours <= 48) return 3;
        return 4;
    }

    public sealed record DiscountTierDisplay(string Range, string Description, int Percent);
}
