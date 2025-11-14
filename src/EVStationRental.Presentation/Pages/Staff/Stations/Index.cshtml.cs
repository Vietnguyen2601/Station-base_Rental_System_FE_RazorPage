using EVStationRental.Common.DTOs.StationDTOs;
using EVStationRental.Services.InternalServices.IServices.IStationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Staff.Stations;

[Authorize(Roles = "Staff")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IStationService _stationService;

    public IndexModel(ILogger<IndexModel> logger, IStationService stationService)
    {
        _logger = logger;
        _stationService = stationService;
    }

    public List<StationWithAvailableVehiclesResponse> Stations { get; set; } = new();

    [BindProperty]
    public CreateStationRequestDTO CreateStationDto { get; set; } = new();

    [BindProperty]
    public UpdateStationRequestDTO UpdateStationDto { get; set; } = new();

    [BindProperty]
    public AddVehiclesToStationRequestDTO AddVehiclesDto { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty]
    public Guid StationIdToUpdate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadStationsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Dữ liệu không hợp lệ";
            await LoadStationsAsync();
            return Page();
        }

        try
        {
            var result = await _stationService.CreateStationAsync(CreateStationDto);

            if (result?.StatusCode == 200)
            {
                TempData["Ok"] = "Thêm trạm thành công";
                return RedirectToPage();
            }

            TempData["Err"] = result?.Message ?? "Không thể thêm trạm";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating station");
            TempData["Err"] = "Lỗi khi thêm trạm";
        }

        await LoadStationsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (!ModelState.IsValid || StationIdToUpdate == Guid.Empty)
        {
            TempData["Err"] = "Dữ liệu không hợp lệ";
            await LoadStationsAsync();
            return Page();
        }

        try
        {
            var result = await _stationService.UpdateStationAsync(StationIdToUpdate, UpdateStationDto);

            if (result?.StatusCode == 200)
            {
                TempData["Ok"] = "Cập nhật trạm thành công";
                return RedirectToPage();
            }

            TempData["Err"] = result?.Message ?? "Không thể cập nhật trạm";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating station");
            TempData["Err"] = "Lỗi khi cập nhật trạm";
        }

        await LoadStationsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Err"] = "ID không hợp lệ";
            return RedirectToPage();
        }

        try
        {
            var result = await _stationService.SoftDeleteStationAsync(id);

            if (result?.StatusCode == 200)
            {
                TempData["Ok"] = "Xóa trạm thành công";
            }
            else
            {
                TempData["Err"] = result?.Message ?? "Không thể xóa trạm";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting station");
            TempData["Err"] = "Lỗi khi xóa trạm";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddVehiclesAsync()
    {
        if (!ModelState.IsValid || AddVehiclesDto.StationId == Guid.Empty || !AddVehiclesDto.VehicleIds.Any())
        {
            TempData["Err"] = "Dữ liệu không hợp lệ";
            await LoadStationsAsync();
            return Page();
        }

        try
        {
            var result = await _stationService.AddVehiclesToStationAsync(AddVehiclesDto);

            if (result?.StatusCode == 200)
            {
                TempData["Ok"] = "Thêm xe vào trạm thành công";
                return RedirectToPage();
            }

            TempData["Err"] = result?.Message ?? "Không thể thêm xe vào trạm";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding vehicles to station");
            TempData["Err"] = "Lỗi khi thêm xe vào trạm";
        }

        await LoadStationsAsync();
        return Page();
    }

    private async Task LoadStationsAsync()
    {
        try
        {
            var result = await _stationService.GetAllStationsAsync();

            if (result?.StatusCode == 200 && result.Data != null)
            {
                var stations = result.Data as List<StationWithAvailableVehiclesResponse>
                    ?? new List<StationWithAvailableVehiclesResponse>();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    stations = stations.Where(s =>
                        s.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        s.Address.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                Stations = stations;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stations");
            Stations = new List<StationWithAvailableVehiclesResponse>();
        }
    }
}
