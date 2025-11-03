using EVStationRental.Common.Enums.EnumModel;
using System;
using System.Collections.Generic;

namespace EVStationRental.Repositories.Models;

public partial class Order
{
    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid VehicleId { get; set; }

    public string OrderCode { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public DateTime? ReturnTime { get; set; }

    public decimal BasePrice { get; set; }

    public decimal TotalPrice { get; set; }

    public Guid? PromotionId { get; set; }

    public Guid? StaffId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Isactive { get; set; }
    public OrderStatus Status { get; set; }


    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual Account Customer { get; set; } = null!;

    public virtual ICollection<DamageReport> DamageReports { get; set; } = new List<DamageReport>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Promotion? Promotion { get; set; }

    public virtual Account? Staff { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
