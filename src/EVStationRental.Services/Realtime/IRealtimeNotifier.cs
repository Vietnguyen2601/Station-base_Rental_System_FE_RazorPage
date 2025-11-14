using System;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.Realtime;

namespace EVStationRental.Services.Realtime;

public interface IRealtimeNotifier
{
    Task NotifyWalletUpdatedAsync(Guid userId, WalletUpdatedPayload payload);
    Task NotifyOrderCreatedAsync(Guid userId, OrderSummaryPayload payload);
    Task NotifyOrderStatusChangedAsync(Guid userId, OrderSummaryPayload payload);
    Task NotifyAccountChangedAsync(AccountSummaryPayload payload);
}
