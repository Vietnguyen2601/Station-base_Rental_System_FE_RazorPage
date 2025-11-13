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
public class RevenueModel : DashboardPageModelBase
{
    private readonly IDashboardService _dashboardService;

    public RevenueModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [BindProperty(SupportsGet = true)]
    public DashboardFilterDTO Filter { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Preset { get; set; }

    public DashboardFilterViewModel FilterVm { get; private set; } = default!;
    public StationRevenueDashboardResponseDTO RevenueData { get; private set; } = new();
    public IReadOnlyList<StationRevenueDTO> StationRevenues => RevenueData.StationRevenues ?? new List<StationRevenueDTO>();
    public int TotalCompleted => StationRevenues.Sum(s => s.CompletedOrders);

    public async Task OnGetAsync()
    {
        var normalizedPreset = DashboardFilterHelper.Normalize(Filter, Preset);
        Preset = normalizedPreset;
        FilterVm = DashboardFilterHelper.BuildViewModel(Filter, normalizedPreset, Url.Page("/Admin/Dashboard/Revenue"));

        RevenueData = await ReadResultAsync<StationRevenueDashboardResponseDTO>(
            () => _dashboardService.GetStationRevenueAsync(Filter))
            ?? new StationRevenueDashboardResponseDTO();
    }
}
