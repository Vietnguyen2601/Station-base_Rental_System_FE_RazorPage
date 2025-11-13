using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.DashboardDTOs;
using EVStationRental.Presentation.Helpers;
using EVStationRental.Presentation.Models.Admin.Dashboard;
using EVStationRental.Services.InternalServices.IServices.IDashboardServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVStationRental.Presentation.Pages.Admin.Dashboard;

[Authorize(Roles = "Admin")]
public class OrdersModel : DashboardPageModelBase
{
    private readonly IDashboardService _dashboardService;

    public OrdersModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [BindProperty(SupportsGet = true)]
    public DashboardFilterDTO Filter { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Preset { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? q { get; set; }

    public DashboardFilterViewModel FilterVm { get; private set; } = default!;
    public List<StationOrderCountItem> Stations { get; private set; } = new();
    public int TotalOrders => Stations.Sum(s => s.TotalOrders);
    public string FilterDescription { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var normalizedPreset = DashboardFilterHelper.Normalize(Filter, Preset);
        Preset = normalizedPreset;

        FilterVm = DashboardFilterHelper.BuildViewModel(
            Filter,
            normalizedPreset,
            Url.Page("/Admin/Dashboard/Orders"),
            showSearchBox: true,
            searchInputName: nameof(q),
            searchQuery: q);

        var response = await ReadResultAsync<StationOrderCountResponse>(
            () => _dashboardService.GetStationOrderCountAsync(Filter))
            ?? new StationOrderCountResponse();

        var stations = response.Stations ?? new List<StationOrderCountItem>();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            stations = stations
                .Where(s => s.StationName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                         || s.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        Stations = stations;
        FilterDescription = response.FilterDescription ?? string.Empty;
    }
}
