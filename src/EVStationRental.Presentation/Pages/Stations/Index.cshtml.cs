using EVStationRental.Repositories.Models;
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
        if (result?.Data is IEnumerable<Station> stations)
        {
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
        else
        {
            TempData["Err"] = result?.Message ?? "Không thể tải danh sách trạm";
            Items = new List<StationVm>();
        }
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
}
