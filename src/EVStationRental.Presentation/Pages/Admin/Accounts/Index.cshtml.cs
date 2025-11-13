using System.Linq;
using System.Text.Json;
using EVStationRental.Common.DTOs;
using EVStationRental.Presentation.Models.Admin.Accounts;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IAccountServices;
using EVStationRental.Services.InternalServices.IServices.IRolesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Admin.Accounts;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly IRolesServices _rolesService;

    public IndexModel(IAccountService accountService, IRolesServices rolesService)
    {
        _accountService = accountService;
        _rolesService = rolesService;
    }

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }

    public List<AccountVm> Items { get; private set; } = new();
    public List<RoleOptionVm> Roles { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var accountsResult = await _accountService.GetAllAccountsAsync();
        Items = MapAccounts(accountsResult);

        var rolesResult = await _rolesService.GetAllRolesAsync();
        Roles = MapRoles(rolesResult);

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim().ToLowerInvariant();
            Items = Items
                .Where(x => x.Username.ToLower().Contains(term) ||
                            x.Email.ToLower().Contains(term) ||
                            x.Roles.Any(r => r.ToLower().Contains(term)))
                .ToList();
        }

        Items = Items.OrderByDescending(x => x.CreatedAt).ToList();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostUpdateRoleAsync(Guid accountId, Guid roleId)
    {
        var response = await _accountService.SetAccountRolesAsync(accountId, new List<Guid> { roleId });
        var key = response?.StatusCode is >= 200 and < 300 ? "Success" : "Error";
        TempData[key] = response?.Message ?? (key == "Success" ? "Đã cập nhật vai trò." : "Không thể cập nhật vai trò.");
        return RedirectToPage(new { q = Q });
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync(Guid accountId)
    {
        var response = await _accountService.SoftDeleteAccountAsync(accountId);
        var key = response?.StatusCode is >= 200 and < 300 ? "Success" : "Error";
        TempData[key] = response?.Message ?? (key == "Success" ? "Đã xoá tài khoản." : "Không thể xoá tài khoản.");
        return RedirectToPage(new { q = Q });
    }

    private static List<AccountVm> MapAccounts(IServiceResult? result)
    {
        if (result?.Data is null) return new List<AccountVm>();
        var json = JsonSerializer.Serialize(result.Data);
        var dtos = JsonSerializer.Deserialize<List<ViewAccountDTO>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<ViewAccountDTO>();

        return dtos.Select(dto => new AccountVm
        {
            AccountId = dto.AccountId,
            Username = dto.Username,
            Email = dto.Email,
            Roles = dto.RoleName,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt
        }).ToList();
    }

    private static List<RoleOptionVm> MapRoles(IServiceResult? result)
    {
        if (result?.Data is null) return new List<RoleOptionVm>();
        var json = JsonSerializer.Serialize(result.Data);
        var dtos = JsonSerializer.Deserialize<List<RoleRecord>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<RoleRecord>();

        return dtos
            .Where(r => r.Isactive)
            .Select(r => new RoleOptionVm(r.RoleId, r.RoleName))
            .ToList();
    }

    private sealed record RoleRecord(Guid RoleId, string RoleName, bool Isactive);
    public sealed record RoleOptionVm(Guid RoleId, string RoleName);
}
