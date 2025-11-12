using System.Security.Claims;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Customer;

[Authorize]
public class DashboardModel : PageModel
{
    private static readonly OrderStatus[] ActiveStatuses =
    {
        OrderStatus.PENDING,
        OrderStatus.CONFIRMED,
        OrderStatus.ONGOING
    };

    private readonly IOrderService _orderService;

    public DashboardModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public List<OrderItemVm> ActiveOrders { get; private set; } = new();
    public List<OrderItemVm> HistoryOrders { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            TempData["Err"] = "Phiên đăng nhập không hợp lệ";
            return RedirectToPage("/Auth/Login");
        }

        var result = await _orderService.GetOrdersByCustomerIdAsync(userId);
        if (result?.Data is not IEnumerable<ViewOrderResponseDTO> orders)
        {
            TempData["Err"] = result?.Message ?? "Không thể tải đơn hàng";
            return Page();
        }

        var mapped = orders.Select(o => new OrderItemVm(
            o.OrderId,
            o.OrderCode,
            o.VehicleId,
            o.StartTime,
            o.EndTime,
            o.TotalPrice,
            o.Status)).ToList();

        ActiveOrders = mapped.Where(o => ActiveStatuses.Contains(o.Status)).ToList();
        HistoryOrders = mapped.Where(o => !ActiveStatuses.Contains(o.Status)).ToList();

        return Page();
    }

    public record OrderItemVm(Guid OrderId, string OrderCode, Guid VehicleId, DateTime StartTime, DateTime? EndTime, decimal TotalPrice, OrderStatus Status);
}
