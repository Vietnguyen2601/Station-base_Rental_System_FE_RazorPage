using System;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.Realtime;
using EVStationRental.Presentation.Hubs;
using EVStationRental.Services.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EVStationRental.Presentation.Services;

public sealed class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<RealtimeHub> _hubContext;
    private readonly ILogger<RealtimeNotifier> _logger;

    public RealtimeNotifier(IHubContext<RealtimeHub> hubContext, ILogger<RealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task NotifyWalletUpdatedAsync(Guid accountId, WalletUpdatedPayload payload) =>
        SendAsync(RealtimeGroups.UserPrefix + accountId, "WalletUpdated", payload);

    public Task NotifyOrderCreatedAsync(Guid accountId, OrderSummaryPayload payload) =>
        Task.WhenAll(
            SendAsync(RealtimeGroups.UserPrefix + accountId, "OrderCreated", payload),
            SendAsync(RealtimeGroups.StaffGroup, "OrderCreated", payload)
        );

    public Task NotifyOrderStatusChangedAsync(Guid accountId, OrderSummaryPayload payload) =>
        Task.WhenAll(
            SendAsync(RealtimeGroups.UserPrefix + accountId, "OrderStatusChanged", payload),
            SendAsync(RealtimeGroups.StaffGroup, "OrderStatusChanged", payload)
        );

    public Task NotifyOrderUpdatedByStaffAsync(OrderStaffUpdatePayload payload) =>
        SendAsync(RealtimeGroups.StaffGroup, "OrderUpdatedByStaff", payload);

    public Task NotifyVehicleUpdatedAsync(VehicleUpdatedPayload payload) =>
        SendAsync(RealtimeGroups.StaffGroup, "VehicleUpdated", payload);

    public Task NotifyStationUpdatedAsync(StationUpdatedPayload payload) =>
        SendAsync(RealtimeGroups.StaffGroup, "StationUpdated", payload);

    public Task NotifyAccountChangedAsync(AccountSummaryPayload payload) =>
        SendAsync(RealtimeGroups.AdminGroup, "AccountChanged", payload);

    private Task SendAsync(string group, string method, object payload)
    {
        try
        {
            return _hubContext.Clients.Group(group).SendAsync(method, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send realtime event {Method} to {Group}", method, group);
            return Task.CompletedTask;
        }
    }
}
