using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVStationRental.Repositories.Models;

/// <summary>
/// Junction table for many-to-many relationship between Account and Role
/// Note: This is currently not mapped to the database. 
/// The current database schema uses a simple one-to-many relationship (Account.RoleId -> Role.RoleId)
/// </summary>
[NotMapped]
public partial class AccountRole
{
    public Guid AccountId { get; set; }

    public Guid RoleId { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
