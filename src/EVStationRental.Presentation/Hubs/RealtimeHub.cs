using System.Security.Claims;
using System.Threading.Tasks;
using EVStationRental.Services.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EVStationRental.Presentation.Hubs;

[Authorize]
public class RealtimeHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.UserPrefix + userId);
        }

        if (Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.AdminGroup);
        }

        if (Context.User?.IsInRole("Staff") == true || Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.StaffGroup);
        }

        await base.OnConnectedAsync();
    }

    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
    }
}
