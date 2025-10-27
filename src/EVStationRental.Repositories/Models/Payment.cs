using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using EVStationRental.Common.Enums.EnumModel;

namespace EVStationRental.Repositories.Models;

public partial class Payment
{
    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string PaymentMethod { get; set; } = null!;

    [Column("payment_type")]
    public PaymentType PaymentType { get; set; } = PaymentType.DEPOSIT; // Default to DEPOSIT

    public string Status { get; set; } = null!;

    public string? GatewayTxId { get; set; }

    public string? GatewayResponse { get; set; }

    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Isactive { get; set; }

    public virtual Order Order { get; set; } = null!;
}
