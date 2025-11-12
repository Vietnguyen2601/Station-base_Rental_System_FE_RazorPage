using System.Linq;
using EVStationRental.Repositories.Models;
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
        var stations = response?.Data as IEnumerable<Station> ?? Enumerable.Empty<Station>();

        Stations = stations
            .Where(s => s.Isactive)
            .OrderBy(s => s.Name)
            .Select(s => new StationVm(s.StationId, s.Name, s.Address))
            .ToList();
    }

    public sealed record StationVm(Guid StationId, string Name, string Address);
}
