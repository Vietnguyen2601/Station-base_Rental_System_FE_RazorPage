using System.Security.Claims;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Wallet;

[Authorize(Roles = "Customer")]
public class PayOSReturnModel : PageModel
{
    private readonly IWalletService _walletService;
    private readonly ILogger<PayOSReturnModel> _logger;

    public PayOSReturnModel(IWalletService walletService, ILogger<PayOSReturnModel> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public string? Status { get; private set; }
    public string? Message { get; private set; }
    public decimal? Amount { get; private set; }
    public decimal? NewBalance { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        [FromQuery] long? orderCode,
        [FromQuery] string? status,
        [FromQuery] string? cancel)
    {
        try
        {
            // Handle cancellation
            if (!string.IsNullOrEmpty(cancel) && cancel.ToLower() == "true")
            {
                Status = "CANCELLED";
                Message = "Bạn đã hủy giao dịch nạp tiền.";
                return Page();
            }

            if (!orderCode.HasValue || string.IsNullOrEmpty(status))
            {
                Status = "ERROR";
                Message = "Thông tin thanh toán không hợp lệ.";
                return Page();
            }

            var accountId = GetCurrentAccountId();
            if (accountId == Guid.Empty)
            {
                return Challenge();
            }

            // Process PayOS return
            var result = await _walletService.HandlePayOSWalletReturnAsync(orderCode.Value, status);

            if (result.StatusCode is >= 200 and < 300)
            {
                Status = "SUCCESS";
                Message = result.Message ?? "Nạp tiền thành công!";

                // Extract data from result
                if (result.Data != null)
                {
                    var dataType = result.Data.GetType();
                    var amountProp = dataType.GetProperty("Amount");
                    var balanceProp = dataType.GetProperty("NewBalance");

                    if (amountProp != null)
                        Amount = (decimal?)amountProp.GetValue(result.Data);
                    
                    if (balanceProp != null)
                        NewBalance = (decimal?)balanceProp.GetValue(result.Data);
                }

                TempData["Success"] = Message;
            }
            else
            {
                Status = "FAILED";
                Message = result.Message ?? "Giao dịch không thành công.";
                TempData["Error"] = Message;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS return for OrderCode {OrderCode}", orderCode);
            Status = "ERROR";
            Message = "Đã xảy ra lỗi khi xử lý thanh toán. Vui lòng liên hệ hỗ trợ.";
            return Page();
        }
    }

    private Guid GetCurrentAccountId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var accountId) ? accountId : Guid.Empty;
    }
}
