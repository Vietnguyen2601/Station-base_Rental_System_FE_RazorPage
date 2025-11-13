using System.Linq;
using EVStationRental.Common.DTOs.StationDTOs;
using EVStationRental.Common.DTOs.VehicleModelDTOs;
using EVStationRental.Services.InternalServices.IServices.IStationServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.VehicleModels;

[Authorize(Roles = "Customer")]
public class BrowseModel : PageModel
{
    private readonly IVehicleModelService _vehicleModelService;
    private readonly IStationService _stationService;

    public BrowseModel(IVehicleModelService vehicleModelService, IStationService stationService)
    {
        _vehicleModelService = vehicleModelService;
        _stationService = stationService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid StationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? q { get; set; }

    public List<ModelVm> Items { get; private set; } = new();
    public string StationName { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (StationId == Guid.Empty)
        {
            return RedirectToPage("/Stations/Browse");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var activeModelsResult = await _vehicleModelService.GetActiveVehicleModelsAsync();
        var models = activeModelsResult?.Data as IEnumerable<ViewVehicleModelResponseDTO> ?? Enumerable.Empty<ViewVehicleModelResponseDTO>();

        var items = new List<ModelVm>();

        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stationResult = await _stationService.GetStationsByVehicleModelAsync(model.VehicleModelId);
            if (stationResult?.Data is not IEnumerable<StationWithAvailableVehiclesResponse> stations)
            {
                continue;
            }

            var stationInfo = stations.FirstOrDefault(s => s.StationId == StationId);
            if (stationInfo is null || stationInfo.AvailableVehicleCount <= 0)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(StationName))
            {
                StationName = stationInfo.Name;
            }

            items.Add(new ModelVm(
                model.VehicleModelId,
                model.Name,
                model.Manufacturer,
                model.Specs ?? string.Empty,
                model.PricePerHour,
                stationInfo.AvailableVehicleCount));
        }

        Items = items
            .OrderByDescending(vm => vm.AvailableCount)
            .ThenBy(vm => vm.ModelName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(q))
        {
            Items = Items
                .Where(item => item.ModelName.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        StationName = string.IsNullOrWhiteSpace(StationName) ? "Trạm đã chọn" : StationName;

        return Page();
    }

    public sealed record ModelVm(Guid ModelId, string ModelName, string Manufacturer, string Specs, decimal PricePerHour, int AvailableCount);
}
