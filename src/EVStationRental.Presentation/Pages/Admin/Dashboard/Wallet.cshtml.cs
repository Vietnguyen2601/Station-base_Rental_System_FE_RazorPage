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
public class WalletModel : DashboardPageModelBase
{
    private readonly IDashboardService _dashboardService;

    public WalletModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [BindProperty(SupportsGet = true)]
    public DashboardFilterDTO Filter { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Preset { get; set; }

    public DashboardFilterViewModel FilterVm { get; private set; } = default!;
    public StationRevenueDashboardResponseDTO RevenueData { get; private set; } = new();
    public StationOrderCountResponse Orders { get; private set; } = new();
    public IReadOnlyList<TopStationRevenueDTO> TopStations { get; private set; } = new List<TopStationRevenueDTO>();

    public async Task OnGetAsync()
    {
        var normalizedPreset = DashboardFilterHelper.Normalize(Filter, Preset);
        Preset = normalizedPreset;
        FilterVm = DashboardFilterHelper.BuildViewModel(Filter, normalizedPreset, Url.Page("/Admin/Dashboard/Wallet"));

        RevenueData = await ReadResultAsync<StationRevenueDashboardResponseDTO>(
            () => _dashboardService.GetStationRevenueAsync(Filter))
            ?? new StationRevenueDashboardResponseDTO();

        Orders = await ReadResultAsync<StationOrderCountResponse>(
            () => _dashboardService.GetStationOrderCountAsync(Filter))
            ?? new StationOrderCountResponse();

        var topStationsResponse = await ReadResultAsync<TopStationsDashboardResponseDTO>(
            () => _dashboardService.GetTopStationsByRevenueAsync(Filter, 5))
            ?? new TopStationsDashboardResponseDTO();

        TopStations = topStationsResponse.TopStations ?? new List<TopStationRevenueDTO>();
    }
}
