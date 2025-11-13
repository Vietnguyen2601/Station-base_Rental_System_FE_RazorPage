using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EVStationRental.Common.DTOs.FeedbackDTOs;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Services.InternalServices.IServices.IFeedbackServices;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Feedback;

[Authorize(Roles = "Customer")]
public class CreateModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IFeedbackService _feedbackService;

    public CreateModel(IOrderService orderService, IFeedbackService feedbackService)
    {
        _orderService = orderService;
        _feedbackService = feedbackService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid OrderId { get; set; }

    [BindProperty]
    public CreateFeedbackInputModel Input { get; set; } = new();

    public OrderSummaryVm? OrderSummary { get; private set; }

    public bool AlreadySubmitted { get; private set; }
    public ViewFeedbackResponseDTO? ExistingFeedback { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid orderId)
    {
        OrderId = orderId;
        var accountId = GetCurrentAccountId();
        if (accountId == Guid.Empty)
        {
            TempData["Error"] = "Phiên đăng nhập không hợp lệ.";
            return RedirectToPage("/Orders/My");
        }

        var loadResult = await LoadOrderAsync(accountId);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        await LoadExistingFeedbackAsync(accountId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var accountId = GetCurrentAccountId();
        if (accountId == Guid.Empty)
        {
            TempData["Error"] = "Phiên đăng nhập không hợp lệ.";
            return RedirectToPage("/Orders/My");
        }

        var loadResult = await LoadOrderAsync(accountId);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        if (!ModelState.IsValid)
        {
            await LoadExistingFeedbackAsync(accountId);
            return Page();
        }

        var existing = await _feedbackService.GetFeedbackByOrderIdAsync(OrderId);
        if (existing?.Data is ViewFeedbackResponseDTO dto && dto.CustomerId == accountId)
        {
            TempData["Info"] = "Bạn đã đánh giá đơn hàng này.";
            return RedirectToPage("/Orders/Details", new { id = OrderId });
        }

        var request = new CreateFeedbackRequestDTO
        {
            CustomerId = accountId,
            OrderId = OrderId,
            Rating = Input.Rating,
            Comment = Input.Comment?.Trim() ?? string.Empty
        };

        var createResult = await _feedbackService.CreateFeedbackAsync(request);
        if (createResult.StatusCode is >= 200 and < 300)
        {
            TempData["Success"] = "Gửi đánh giá thành công.";
            return RedirectToPage("/Orders/Details", new { id = OrderId });
        }

        TempData["Error"] = createResult.Message ?? "Không thể gửi đánh giá.";
        await LoadExistingFeedbackAsync(accountId);
        return Page();
    }

    private async Task<IActionResult?> LoadOrderAsync(Guid accountId)
    {
        var orderResult = await _orderService.GetOrderByIdAsync(OrderId);
        if (orderResult?.Data is not ViewOrderResponseDTO order || order.CustomerId != accountId)
        {
            TempData["Error"] = "Bạn không có quyền đánh giá đơn này.";
            return RedirectToPage("/Orders/My");
        }

        if (order.Status != OrderStatus.COMPLETED)
        {
            TempData["Error"] = "Chỉ đánh giá đơn đã hoàn thành.";
            return RedirectToPage("/Orders/My");
        }

        var vehicleReference = order.VehicleId != Guid.Empty
            ? order.VehicleId.ToString("N")[..8].ToUpperInvariant()
            : "N/A";

        OrderSummary = new OrderSummaryVm
        {
            OrderCode = order.OrderCode,
            VehicleReference = vehicleReference,
            StartTime = order.StartTime,
            EndTime = order.EndTime,
            TotalPrice = order.TotalPrice
        };

        return null;
    }

    private async Task LoadExistingFeedbackAsync(Guid accountId)
    {
        var feedbackResult = await _feedbackService.GetFeedbackByOrderIdAsync(OrderId);
        if (feedbackResult?.Data is ViewFeedbackResponseDTO dto && dto.CustomerId == accountId)
        {
            AlreadySubmitted = true;
            ExistingFeedback = dto;
        }
    }

    private Guid GetCurrentAccountId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var accountId) ? accountId : Guid.Empty;
    }

    public class CreateFeedbackInputModel
    {
        [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao từ 1 đến 5.")]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public class OrderSummaryVm
    {
        public string OrderCode { get; set; } = string.Empty;
        public string VehicleReference { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
