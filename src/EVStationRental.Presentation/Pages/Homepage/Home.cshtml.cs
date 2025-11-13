using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Homepage;

public class HomeModel : PageModel
{
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    public void OnGet()
    {
        if (IsAuthenticated)
        {
            DisplayName = User.Identity?.Name
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "bạn";
        }
    }
}
