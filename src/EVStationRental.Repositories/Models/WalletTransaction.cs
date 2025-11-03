using System;
using System.Collections.Generic;

namespace EVStationRental.Repositories.Models;

public partial class WalletTransaction
{
    public Guid TransactionId { get; set; }

    public Guid WalletId { get; set; }

    public Guid? OrderId { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool Isactive { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
