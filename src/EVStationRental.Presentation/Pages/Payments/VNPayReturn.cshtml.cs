using EVStationRental.Common.DTOs.PaymentDTOs;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Payments;

[AllowAnonymous]
public class VNPayReturnModel : PageModel
{
    private readonly IWalletService _walletService;
    private readonly ILogger<VNPayReturnModel> _logger;

    public VNPayReturnModel(IWalletService walletService, ILogger<VNPayReturnModel> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public string Status { get; private set; } = "PENDING";
    public string Message { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var returnDto = BuildReturnDto();
        if (string.IsNullOrEmpty(returnDto.vnp_TxnRef))
        {
            Status = "FAILED";
            Message = "Thiếu thông tin giao dịch VNPay.";
            TempData["Error"] = Message;
            return Page();
        }

        var result = await _walletService.HandleVNPayWalletReturnAsync(returnDto);
        if (result.StatusCode is >= 200 and < 300)
        {
            Status = "COMPLETED";
            Message = result.Message ?? "Nạp tiền thành công.";
            TempData["Success"] = Message;
        }
        else
        {
            Status = "FAILED";
            Message = result.Message ?? "Thanh toán thất bại.";
            TempData["Error"] = Message;
        }

        _logger.LogInformation("VNPay return processed with status {Status}: {Message}", Status, Message);
        return Page();
    }

    private VNPayReturnDTO BuildReturnDto()
    {
        var dto = new VNPayReturnDTO();
        foreach (var prop in typeof(VNPayReturnDTO).GetProperties())
        {
            var key = prop.Name;
            var value = Request.Query[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                prop.SetValue(dto, value.ToString());
            }
        }
        return dto;
    }
}
