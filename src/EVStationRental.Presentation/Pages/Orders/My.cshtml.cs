using System.Security.Claims;
using EVStationRental.Common.DTOs.FeedbackDTOs;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Services.InternalServices.IServices.IFeedbackServices;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Orders;

[Authorize(Roles = "Customer")]
public class MyModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IFeedbackService _feedbackService;

    public MyModel(IOrderService orderService, IFeedbackService feedbackService)
    {
        _orderService = orderService;
        _feedbackService = feedbackService;
    }

    public IList<ViewOrderResponseDTO> Orders { get; private set; } = new List<ViewOrderResponseDTO>();
    public HashSet<Guid> FeedbackOrderIds { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOrdersAsync();
        return Page();
    }

    private async Task LoadOrdersAsync()
    {
        var accountId = GetCurrentAccountId();
        if (accountId == Guid.Empty)
        {
            TempData["Error"] = "Phiên đăng nhập không hợp lệ.";
            return;
        }

        var result = await _orderService.GetOrdersByCustomerIdAsync(accountId);
        if (result?.Data is IEnumerable<ViewOrderResponseDTO> orders)
        {
            Orders = orders.OrderByDescending(o => o.CreatedAt).ToList();
        }
        else
        {
            TempData["Error"] = result?.Message ?? "Không thể tải danh sách đơn.";
        }

        var feedbackResult = await _feedbackService.GetFeedbacksByCustomerIdAsync(accountId);
        if (feedbackResult?.Data is IEnumerable<ViewFeedbackResponseDTO> feedbacks)
        {
            FeedbackOrderIds = feedbacks.Select(f => f.OrderId).ToHashSet();
        }
    }

    private Guid GetCurrentAccountId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var accountId) ? accountId : Guid.Empty;
    }
}
