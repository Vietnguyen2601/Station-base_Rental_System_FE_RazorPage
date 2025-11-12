using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Vehicles;

public class BrowseModel : PageModel
{
    private readonly IVehicleService _vehicleService;

    public BrowseModel(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    public List<VehicleCardVm> Vehicles { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? q { get; set; }

    public async Task OnGetAsync()
    {
        var result = await _vehicleService.GetActiveVehiclesAsync();
        if (result?.Data is IEnumerable<ViewVehicleResponse> data)
        {
            var cards = data.Select(v => new VehicleCardVm(
                v.VehicleId,
                v.ModelName,
                v.Manufacturer,
                v.PricePerHour,
                v.SerialNumber,
                v.Color ?? "N/A",
                v.Specs ?? string.Empty)).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                cards = cards
                    .Where(c => c.ModelName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                c.Manufacturer.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                c.SerialNumber.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Vehicles = cards;
        }
        else
        {
            TempData["Err"] = result?.Message ?? "Không thể tải danh sách xe";
        }
    }

    public record VehicleCardVm(Guid VehicleId, string ModelName, string Manufacturer, decimal PricePerHour, string SerialNumber, string Color, string Specs);
}
