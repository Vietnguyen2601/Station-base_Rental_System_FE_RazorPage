using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Presentation.Models.Admin.Accounts;

public sealed class AccountVm
{
    public Guid AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public string PrimaryRole => Roles.FirstOrDefault() ?? string.Empty;
}

public sealed class AccountRoleVm
{
    public Guid AccountId { get; set; }

    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = "Admin";
}
