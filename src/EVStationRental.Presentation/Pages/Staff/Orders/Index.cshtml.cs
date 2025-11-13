using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using EVStationRental.Services.InternalServices.IServices.IAccountServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.DTOs;
using EVStationRental.Common.DTOs.VehicleDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVStationRental.Presentation.Pages.Staff.Orders;

[Authorize(Roles = "Staff")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IAccountService _accountService;
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IOrderService orderService,
        IAccountService accountService,
        IVehicleService vehicleService,
        ILogger<IndexModel> logger)
    {
        _orderService = orderService;
        _accountService = accountService;
        _vehicleService = vehicleService;
        _logger = logger;
    }

    public List<ViewOrderResponseDTO> Orders { get; set; } = new();
    public Dictionary<Guid, string> CustomerNames { get; set; } = new();
    public Dictionary<Guid, string> VehicleSerialNumbers { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; } = "newest";

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        await LoadAccountsAndVehiclesAsync();

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadOrdersAsync();
            return Page();
        }

        var result = await _orderService.GetOrderByOrderCodeAsync(SearchQuery);
        if (result.StatusCode == 200 && result.Data is ViewOrderResponseDTO order)
        {
            Orders = new List<ViewOrderResponseDTO> { order };
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không tìm thấy đơn hàng";
            await LoadOrdersAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStartOrderAsync(Guid orderId)
    {
        var result = await _orderService.StartOrderAsync(orderId);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = "Đã cập nhật trạng thái đơn hàng sang ONGOING";
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể cập nhật trạng thái đơn hàng";
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        await LoadAccountsAndVehiclesAsync();
        await LoadOrdersAsync();
    }

    private async Task LoadAccountsAndVehiclesAsync()
    {
        // Load accounts
        var accountResult = await _accountService.GetAllAccountsAsync();
        if (accountResult.StatusCode == 200 && accountResult.Data is List<ViewAccountDTO> accounts)
        {
            CustomerNames = accounts.ToDictionary(a => a.AccountId, a => a.Username);
        }
        else
        {
            _logger.LogWarning("Failed to load accounts: {Message}", accountResult.Message);
            CustomerNames = new Dictionary<Guid, string>();
        }

        // Load vehicles
        var vehicleResult = await _vehicleService.GetAllVehiclesAsync();
        if (vehicleResult.StatusCode == 200 && vehicleResult.Data is List<ViewVehicleResponse> vehicles)
        {
            VehicleSerialNumbers = vehicles.ToDictionary(v => v.VehicleId, v => v.SerialNumber);
        }
        else
        {
            _logger.LogWarning("Failed to load vehicles: {Message}", vehicleResult.Message);
            VehicleSerialNumbers = new Dictionary<Guid, string>();
        }
    }

    private async Task LoadOrdersAsync()
    {
        var result = await _orderService.GetAllOrdersAsync();

        if (result.StatusCode == 200 && result.Data is List<ViewOrderResponseDTO> orders)
        {
            // Filter by status if selected
            if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<EVStationRental.Common.Enums.EnumModel.OrderStatus>(StatusFilter, out var status))
            {
                orders = orders.Where(o => o.Status == status).ToList();
            }

            // Sort orders
            Orders = SortOrder switch
            {
                "oldest" => orders.OrderBy(o => o.OrderDate).ToList(),
                "newest" => orders.OrderByDescending(o => o.OrderDate).ToList(),
                "price-high" => orders.OrderByDescending(o => o.TotalPrice).ToList(),
                "price-low" => orders.OrderBy(o => o.TotalPrice).ToList(),
                _ => orders.OrderByDescending(o => o.OrderDate).ToList()
            };
        }
        else
        {
            _logger.LogWarning("Failed to load orders: {Message}", result.Message);
            Orders = new List<ViewOrderResponseDTO>();
        }
    }
}
