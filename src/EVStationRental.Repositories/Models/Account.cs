using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVStationRental.Repositories.Models;

public partial class Account
{
    public Guid AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? ContactNumber { get; set; }

    public Guid RoleId { get; set; }

    public bool Isactive { get; set; }

    // Mapped property for consistency with DTO naming
    [NotMapped]
    public bool IsActive
    {
        get => Isactive;
        set => Isactive = value;
    }

    // These properties are not in the database yet, but needed for the DTOs
    [NotMapped]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public DateTime? UpdatedAt { get; set; }

    // Navigation property for many-to-many relationship (if AccountRole table exists)
    // If not, this will use the Role property instead
    [NotMapped]
    public virtual ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Order> OrderCustomers { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderStaffs { get; set; } = new List<Order>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<StaffRevenue> StaffRevenues { get; set; } = new List<StaffRevenue>();
}
