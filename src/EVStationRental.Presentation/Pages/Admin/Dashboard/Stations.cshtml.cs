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
public class StationsModel : DashboardPageModelBase
{
    private readonly IDashboardService _dashboardService;

    public StationsModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [BindProperty(SupportsGet = true)]
    public DashboardFilterDTO Filter { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Preset { get; set; }

    public DashboardFilterViewModel FilterVm { get; private set; } = default!;
    public StationVehicleUsageResponse StationUsage { get; private set; } = new();
    public IReadOnlyList<VehicleModelUsageDTO> TopVehicles { get; private set; } = new List<VehicleModelUsageDTO>();

    public async Task OnGetAsync()
    {
        var normalizedPreset = DashboardFilterHelper.Normalize(Filter, Preset);
        Preset = normalizedPreset;
        FilterVm = DashboardFilterHelper.BuildViewModel(Filter, normalizedPreset, Url.Page("/Admin/Dashboard/Stations"));

        StationUsage = await ReadResultAsync<StationVehicleUsageResponse>(
            () => _dashboardService.GetStationVehicleUsageAsync(Filter))
            ?? new StationVehicleUsageResponse();

        var vehicleUsage = await ReadResultAsync<VehicleModelUsageDashboardResponseDTO>(
            () => _dashboardService.GetVehicleModelUsageAsync(Filter))
            ?? new VehicleModelUsageDashboardResponseDTO();

        TopVehicles = vehicleUsage.VehicleModels
            .OrderByDescending(v => v.RentalRate)
            .ThenByDescending(v => v.TotalVehicles)
            .Take(10)
            .ToList();
    }
}
