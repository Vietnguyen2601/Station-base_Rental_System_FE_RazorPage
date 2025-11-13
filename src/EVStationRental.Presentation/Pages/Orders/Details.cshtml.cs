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
public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IFeedbackService _feedbackService;

    public DetailsModel(IOrderService orderService, IFeedbackService feedbackService)
    {
        _orderService = orderService;
        _feedbackService = feedbackService;
    }

    public ViewOrderResponseDTO? Order { get; private set; }
    public ViewFeedbackResponseDTO? Feedback { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var accountId = GetCurrentAccountId();
        var result = await _orderService.GetOrderByIdAsync(id);
        if (result?.Data is not ViewOrderResponseDTO dto || dto.CustomerId != accountId)
        {
            TempData["Error"] = "Bạn không có quyền xem đơn này.";
            return RedirectToPage("/Orders/My");
        }

        Order = dto;

        var feedbackResult = await _feedbackService.GetFeedbackByOrderIdAsync(id);
        if (feedbackResult?.Data is ViewFeedbackResponseDTO feedback && feedback.CustomerId == accountId)
        {
            Feedback = feedback;
        }
        return Page();
    }

    private Guid GetCurrentAccountId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var accountId) ? accountId : Guid.Empty;
    }
}
