using System;
using System.Collections.Generic;

namespace EVStationRental.Repositories.Models;

public partial class Wallet
{
    public Guid WalletId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Isactive { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
