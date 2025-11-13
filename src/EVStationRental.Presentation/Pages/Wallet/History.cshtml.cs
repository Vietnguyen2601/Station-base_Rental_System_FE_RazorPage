using System.Security.Claims;
using System.Text.Json;
using EVStationRental.Common.DTOs.WalletDTOs;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Wallet;

[Authorize(Roles = "Customer")]
public class HistoryModel : PageModel
{
    private readonly IWalletService _walletService;

    public HistoryModel(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public decimal Balance { get; private set; }
    public Guid WalletId { get; private set; }
    public IList<WalletTransactionDTO> Transactions { get; private set; } = new List<WalletTransactionDTO>();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId == Guid.Empty)
        {
            TempData["Error"] = "Phiên đăng nhập không hợp lệ.";
            return RedirectToPage("/Auth/Login");
        }

        if (!await EnsureWalletAsync(accountId))
        {
            TempData["Error"] = "Không thể khởi tạo ví của bạn.";
            return RedirectToPage("/Wallet/Topup");
        }

        var historyResult = await _walletService.GetTransactionHistoryAsync(accountId, 1, 200);
        var payload = ConvertData<WalletHistoryPayload>(historyResult?.Data);
        if (payload == null)
        {
            TempData["Error"] = historyResult?.Message ?? "Không thể tải lịch sử ví.";
            return Page();
        }

        WalletId = payload.WalletId;
        Balance = payload.CurrentBalance;
        Transactions = payload.Transactions ?? new List<WalletTransactionDTO>();
        return Page();
    }

    private Guid GetCurrentAccountId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var accountId) ? accountId : Guid.Empty;
    }

    private async Task<bool> EnsureWalletAsync(Guid accountId)
    {
        var balanceResult = await _walletService.GetWalletBalanceAsync(accountId);
        if (balanceResult?.Data is WalletBalanceDTO wallet)
        {
            WalletId = wallet.WalletId;
            Balance = wallet.Balance;
            return true;
        }

        var createResult = await _walletService.CreateWalletForAccountAsync(accountId);
        if (createResult.StatusCode is >= 200 and < 300)
        {
            balanceResult = await _walletService.GetWalletBalanceAsync(accountId);
            if (balanceResult?.Data is WalletBalanceDTO created)
            {
                WalletId = created.WalletId;
                Balance = created.Balance;
                return true;
            }
        }

        return false;
    }

    private static T? ConvertData<T>(object? data)
    {
        if (data is null) return default;
        var json = JsonSerializer.Serialize(data);
        return JsonSerializer.Deserialize<T>(json);
    }

    private sealed class WalletHistoryPayload
    {
        public Guid WalletId { get; set; }
        public decimal CurrentBalance { get; set; }
        public List<WalletTransactionDTO>? Transactions { get; set; }
    }
}
