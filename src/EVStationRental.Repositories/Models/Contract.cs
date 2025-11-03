using System;
using System.Collections.Generic;

namespace EVStationRental.Repositories.Models;

public partial class Contract
{
    public Guid ContractId { get; set; }

    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid VehicleId { get; set; }

    public DateTime ContractDate { get; set; }

    public string FileUrl { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Isactive { get; set; }

    public virtual Account Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
