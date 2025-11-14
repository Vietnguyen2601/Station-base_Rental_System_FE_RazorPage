using System;

namespace EVStationRental.Common.DTOs.Realtime;

public sealed class WalletUpdatedPayload
{
    public Guid WalletId { get; set; }
    public decimal NewBalance { get; set; }
    public decimal LastChangeAmount { get; set; }
    public string LastChangeType { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
