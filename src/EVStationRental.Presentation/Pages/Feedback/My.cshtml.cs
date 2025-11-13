using System.Security.Claims;
using EVStationRental.Common.DTOs.FeedbackDTOs;
using EVStationRental.Services.InternalServices.IServices.IFeedbackServices;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Feedback;

[Authorize(Roles = "Customer")]
public class MyModel : PageModel
{
    private readonly IFeedbackService _feedbackService;
    private readonly IOrderService _orderService;

    public MyModel(IFeedbackService feedbackService, IOrderService orderService)
    {
        _feedbackService = feedbackService;
        _orderService = orderService;
    }

    public IReadOnlyList<FeedbackDisplayVm> Items { get; private set; } = Array.Empty<FeedbackDisplayVm>();

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public int TotalItems { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var accountId = GetCurrentAccountId();
        if (accountId == Guid.Empty)
        {
            TempData["Error"] = "Phiên đăng nhập không hợp lệ.";
            return RedirectToPage("/Auth/Login");
        }

        var result = await _feedbackService.GetFeedbacksByCustomerIdAsync(accountId);
        if (result?.Data is not IEnumerable<ViewFeedbackResponseDTO> list)
        {
            Items = Array.Empty<FeedbackDisplayVm>();
            return Page();
        }

        var ordered = list.OrderByDescending(f => f.CreatedAt).ToList();
        TotalItems = ordered.Count;

        var pageIndex = Math.Max(Page, 1);
        var pageSize = Math.Clamp(PageSize, 5, 50);
        var pageItems = ordered
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var orderDetails = new Dictionary<Guid, (string Code, DateTime Start, DateTime? End)>();
        foreach (var feedback in pageItems)
        {
            var orderResult = await _orderService.GetOrderByIdAsync(feedback.OrderId);
            if (orderResult?.Data is Common.DTOs.OrderDTOs.ViewOrderResponseDTO order)
            {
                orderDetails[feedback.OrderId] = (order.OrderCode, order.StartTime, order.EndTime);
            }
        }

        Items = pageItems
            .Select(f =>
            {
                orderDetails.TryGetValue(f.OrderId, out var info);
                return new FeedbackDisplayVm
                {
                    FeedbackId = f.FeedbackId,
                    OrderId = f.OrderId,
                    OrderCode = info.Code ?? f.OrderId.ToString(),
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt,
                    OrderStart = info.Start,
                    OrderEnd = info.End
                };
            })
            .ToList();

        Page = pageIndex;
        PageSize = pageSize;
        return Page();
    }

    private Guid GetCurrentAccountId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var accountId) ? accountId : Guid.Empty;
    }

    public class FeedbackDisplayVm
    {
        public Guid FeedbackId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime OrderStart { get; set; }
        public DateTime? OrderEnd { get; set; }
    }
}
