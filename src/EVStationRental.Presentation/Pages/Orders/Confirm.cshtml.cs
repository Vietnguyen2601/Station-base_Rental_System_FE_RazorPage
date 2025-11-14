using System;
using System.Security.Claims;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Presentation.Models.Orders;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Orders;

[Authorize(Roles = "Customer")]
public class ConfirmModel : PageModel
{
    private readonly IOrderService _orderService;

    public ConfirmModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [BindProperty]
    public OrderConfirmVm Order { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var result = await _orderService.GetOrderByIdAsync(id);
        if (result?.StatusCode != Const.SUCCESS_READ_CODE || result.Data is not ViewOrderResponseDTO dto)
        {
            TempData["Error"] = result?.Message ?? "Không tìm thấy đơn đặt xe.";
            return RedirectToPage("/Orders/My");
        }

        if (dto.CustomerId != GetCurrentUserId())
        {
            TempData["Error"] = "Bạn không có quyền xem đơn này.";
            return RedirectToPage("/Orders/My");
        }

        Order = new OrderConfirmVm
        {
            OrderId = dto.OrderId,
            OrderCode = dto.OrderCode,
            StationName = dto.StationName,
            StationAddress = dto.StationAddress,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime ?? dto.StartTime,
            BasePrice = dto.BasePrice,
            TotalAfterDiscount = dto.TotalPrice,
            DiscountAmount = dto.BasePrice - dto.TotalPrice,
            DepositAmount = Math.Round(dto.BasePrice * 0.10m, 0)
        };

        return Page();
    }

    private Guid GetCurrentUserId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : Guid.Empty;
    }
}
