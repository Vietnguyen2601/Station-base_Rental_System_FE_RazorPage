using System;
using System.Collections.Generic;
using EVStationRental.Common.DTOs.DashboardDTOs;
using EVStationRental.Presentation.Helpers;

namespace EVStationRental.Presentation.Models.Admin.Dashboard;

public class DashboardFilterViewModel
{
    public string FormId { get; set; } = $"dashboard-filter-{Guid.NewGuid():N}";
    public string FormAction { get; set; } = string.Empty;
    public DashboardFilterDTO Filter { get; set; } = new();
    public string SelectedPreset { get; set; } = DashboardFilterHelper.PresetMonth;
    public IEnumerable<int> YearOptions { get; set; } = Array.Empty<int>();
    public bool ShowSearchBox { get; set; }
    public string SearchInputName { get; set; } = "q";
    public string? SearchQuery { get; set; }
    public string SearchPlaceholder { get; set; } = "Nhập từ khóa...";
    public Dictionary<string, string?> HiddenFields { get; set; } = new();
}
