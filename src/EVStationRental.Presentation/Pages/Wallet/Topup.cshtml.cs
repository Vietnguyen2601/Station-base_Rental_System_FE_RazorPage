using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Security.Claims;
using EVStationRental.Common.DTOs.WalletDTOs;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace EVStationRental.Presentation.Pages.Wallet;

[Authorize(Roles = "Customer")]
public class TopupModel : PageModel
{
    private readonly IWalletService _walletService;
    private readonly ILogger<TopupModel> _logger;
    private readonly IConfiguration _configuration;

    public TopupModel(
        IWalletService walletService,
        ILogger<TopupModel> logger,
        IConfiguration configuration)
    {
        _walletService = walletService;
        _logger = logger;
        _configuration = configuration;
    }

    [BindProperty]
    public WalletTopupVm Input { get; set; } = new();

    public decimal CurrentBalance { get; private set; }
    public Guid WalletId { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Input ??= new WalletTopupVm();
        if (Input.Amount <= 0)
        {
            Input.Amount = 100_000;
        }

        if (!await EnsureWalletAsync())
        {
            TempData["Error"] = "Không thể tải thông tin ví. Vui lòng thử lại.";
            return RedirectToPage("/Wallet/History");
        }

        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        Input ??= new WalletTopupVm();
        if (!ModelState.IsValid)
        {
            await EnsureWalletAsync();
            return Page();
        }

        if (!await EnsureWalletAsync())
        {
            TempData["Error"] = "Không thể khởi tạo ví. Vui lòng thử lại.";
            return RedirectToPage("/Wallet/Topup");
        }

        var returnUrl = BuildAbsolutePageUrl("/Payments/VNPayReturn");
        var cancelUrl = BuildAbsolutePageUrl("/Wallet/History");
        var ipAddress = GetClientIpAddress();

        var response = await _walletService.CreateVNPayUrlByWalletIdAsync(
            WalletId,
            Input.Amount,
            returnUrl,
            cancelUrl,
            ipAddress);

        if (response?.Data is TopUpResponseDTO dto && !string.IsNullOrWhiteSpace(dto.PaymentUrl))
        {
            TempData["Info"] = "Đang chuyển đến VNPay để hoàn tất thanh toán.";
            return Redirect(dto.PaymentUrl);
        }

        TempData["Error"] = response?.Message ?? "Không tạo được giao dịch VNPay.";
        return RedirectToPage("/Wallet/Topup");
    }

    private async Task<bool> EnsureWalletAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return false;
        }

        var walletResult = await _walletService.GetWalletBalanceAsync(userId);
        if (walletResult?.Data is WalletBalanceDTO walletDto)
        {
            WalletId = walletDto.WalletId;
            CurrentBalance = walletDto.Balance;
            return true;
        }

        var createResult = await _walletService.CreateWalletForAccountAsync(userId);
        if (createResult?.StatusCode is >= 200 and < 300)
        {
            walletResult = await _walletService.GetWalletBalanceAsync(userId);
            if (walletResult?.Data is WalletBalanceDTO created)
            {
                WalletId = created.WalletId;
                CurrentBalance = created.Balance;
                return true;
            }
        }

        _logger.LogWarning("Cannot ensure wallet for {AccountId}. Message: {Message}", userId, createResult?.Message);
        return false;
    }

    private Guid GetCurrentUserId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : Guid.Empty;
    }

    private string BuildAbsolutePageUrl(string pagePath)
    {
        var publicBase = _configuration["App:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(publicBase) &&
            Uri.TryCreate(publicBase, UriKind.Absolute, out var baseUri))
        {
            var relative = pagePath.StartsWith('/') ? pagePath[1..] : pagePath;
            return new Uri(baseUri, relative).ToString();
        }

        var scheme = Request?.Scheme ?? "https";
        var host = Request?.Host.Value;
        return Url.Page(pagePath, null, null, scheme, host) ?? string.Empty;
    }

    private string GetClientIpAddress()
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return "127.0.0.1";
        }

        if (remoteIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (remoteIp.IsIPv4MappedToIPv6)
            {
                return remoteIp.MapToIPv4().ToString();
            }

            return "127.0.0.1";
        }

        return remoteIp.ToString();
    }

    public sealed class WalletTopupVm
    {
        [Display(Name = "Số tiền nạp")]
        [Required(ErrorMessage = "Vui lòng nhập số tiền nạp.")]
        [Range(10000, 50000000, ErrorMessage = "Số tiền phải từ 10.000 đến 50.000.000 VNĐ.")]
        public decimal Amount { get; set; } = 100000;
    }
}
