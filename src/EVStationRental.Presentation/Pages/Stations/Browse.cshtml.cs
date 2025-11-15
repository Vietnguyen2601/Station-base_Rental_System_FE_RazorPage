using System.Linq;
using System.Text.Json;
using EVStationRental.Services.InternalServices.IServices.IStationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Stations;

[Authorize(Roles = "Customer")]
public class BrowseModel : PageModel
{
    private readonly IStationService _stationService;

    public BrowseModel(IStationService stationService) => _stationService = stationService;

    public IReadOnlyList<StationVm> Stations { get; private set; } = Array.Empty<StationVm>();

    public async Task OnGet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = await _stationService.GetActiveStationsAsync();
        var stations = MapStations(response?.Data);

        Stations = stations
            .Where(s => s.Isactive)
            .OrderBy(s => s.Name)
            .Select(s => new StationVm(s.StationId, s.Name, s.Address))
            .ToList();
    }

    public sealed record StationVm(Guid StationId, string Name, string Address);

    private static IReadOnlyList<StationSnapshot> MapStations(object? data)
    {
        if (data is null)
        {
            return Array.Empty<StationSnapshot>();
        }

        try
        {
            var json = JsonSerializer.Serialize(data);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<StationSnapshot>>(json, options) ?? new List<StationSnapshot>();
        }
        catch
        {
            return Array.Empty<StationSnapshot>();
        }
    }

    private sealed record StationSnapshot
    {
        public Guid StationId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public bool Isactive { get; init; }
    }
}
