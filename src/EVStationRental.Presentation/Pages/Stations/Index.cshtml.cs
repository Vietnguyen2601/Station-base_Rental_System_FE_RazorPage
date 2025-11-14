using System.Text.Json;
using EVStationRental.Services.InternalServices.IServices.IStationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Stations;

[Authorize(Roles = "Admin,Staff")]
public class IndexModel : PageModel
{
    private readonly IStationService _stationService;

    public IndexModel(IStationService stationService)
    {
        _stationService = stationService;
    }

    public List<StationVm> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var result = await _stationService.GetAllStationsAsync();
        var stations = MapStations(result?.Data);
        if (!stations.Any())
        {
            TempData["Err"] = result?.Message ?? "Không thể tải danh sách trạm";
        }

        Items = stations.Select(s => new StationVm
        {
            StationId = s.StationId,
            Name = s.Name,
            Address = s.Address,
            Capacity = s.Capacity,
            Lat = s.Lat,
            Long = s.Long,
            IsActive = s.Isactive
        }).ToList();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id, bool isActive)
    {
        var response = await _stationService.UpdateIsActiveAsync(id, isActive);
        TempData[response?.StatusCode == 200 ? "Ok" : "Err"] = response?.Message ?? "Không thể cập nhật trạng thái";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var response = await _stationService.SoftDeleteStationAsync(id);
        TempData[response?.StatusCode == 200 ? "Ok" : "Err"] = response?.Message ?? "Không thể xóa trạm";
        return RedirectToPage();
    }

    public sealed class StationVm
    {
        public Guid StationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Lat { get; set; }
        public decimal Long { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
    }

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
        public decimal Lat { get; init; }
        public decimal Long { get; init; }
        public int Capacity { get; init; }
        public bool Isactive { get; init; }
    }
}
