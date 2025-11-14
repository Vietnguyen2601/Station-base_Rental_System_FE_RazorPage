using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.InternalServices.IServices.IStationServices;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.DTOs.VehicleModelDTOs;
using EVStationRental.Common.DTOs.StationDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVStationRental.Presentation.Pages.Staff.Vehicles;

[Authorize(Roles = "Staff")]
public class IndexModel : PageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IVehicleModelService _vehicleModelService;
    private readonly IStationService _stationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IVehicleService vehicleService,
        IVehicleModelService vehicleModelService,
        IStationService stationService,
        ILogger<IndexModel> logger)
    {
        _vehicleService = vehicleService;
        _vehicleModelService = vehicleModelService;
        _stationService = stationService;
        _logger = logger;
    }

    public List<ViewVehicleResponse> Vehicles { get; set; } = new();
    public List<SelectListItem> VehicleModels { get; set; } = new();
    public List<SelectListItem> Stations { get; set; } = new();

    [BindProperty]
    public CreateVehicleRequestDTO CreateVehicleDto { get; set; } = new();

    [BindProperty]
    public UpdateVehicleRequestDTO UpdateVehicleDto { get; set; } = new();

    [BindProperty]
    public Guid VehicleIdToUpdate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        await LoadVehicleModelsAsync();
        await LoadStationsAsync();
        await LoadVehiclesAsync();
    }

    private async Task LoadVehiclesAsync()
    {
        var result = await _vehicleService.GetAllVehiclesAsync();

        if (result.StatusCode == 200 && result.Data is List<ViewVehicleResponse> vehicles)
        {
            Vehicles = vehicles;

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                Vehicles = Vehicles.Where(v =>
                    v.SerialNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    v.ModelName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (v.StationName != null && v.StationName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }
        }
        else
        {
            _logger.LogWarning("Failed to load vehicles: {Message}", result.Message);
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Dữ liệu không hợp lệ";
            await LoadVehicleModelsAsync();
            await LoadStationsAsync();
            await LoadVehiclesAsync();
            return Page();
        }

        var result = await _vehicleService.CreateVehicleAsync(CreateVehicleDto);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = "Tạo xe mới thành công";
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể tạo xe mới";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (VehicleIdToUpdate == Guid.Empty)
        {
            TempData["Err"] = "ID xe không hợp lệ";
            return RedirectToPage();
        }

        var result = await _vehicleService.UpdateVehicleAsync(VehicleIdToUpdate, UpdateVehicleDto);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = "Cập nhật xe thành công";
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể cập nhật xe";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Err"] = "ID xe không hợp lệ";
            return RedirectToPage();
        }

        var result = await _vehicleService.SoftDeleteVehicleAsync(id);

        if (result.StatusCode == 200)
        {
            TempData["Ok"] = "Xóa xe thành công";
        }
        else
        {
            TempData["Err"] = result.Message ?? "Không thể xóa xe";
        }

        return RedirectToPage();
    }

    private async Task LoadVehicleModelsAsync()
    {
        try
        {
            var result = await _vehicleModelService.GetAllVehicleModelsAsync();

            if (result?.StatusCode == 200 && result.Data != null)
            {
                var models = result.Data as List<ViewVehicleModelResponseDTO> ?? new List<ViewVehicleModelResponseDTO>();
                VehicleModels = models.Select(m => new SelectListItem
                {
                    Value = m.VehicleModelId.ToString(),
                    Text = $"{m.Name} ({m.Manufacturer})"
                }).ToList();
            }
            else
            {
                VehicleModels = new List<SelectListItem>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicle models");
            VehicleModels = new List<SelectListItem>();
        }
    }

    private async Task LoadStationsAsync()
    {
        try
        {
            var result = await _stationService.GetAllStationsAsync();

            if (result?.StatusCode == 200 && result.Data != null)
            {
                var stations = result.Data as List<StationWithAvailableVehiclesResponse> ?? new List<StationWithAvailableVehiclesResponse>();
                Stations = stations.Select(s => new SelectListItem
                {
                    Value = s.StationId.ToString(),
                    Text = $"{s.Name} - {s.Address}"
                }).ToList();
            }
            else
            {
                Stations = new List<SelectListItem>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stations");
            Stations = new List<SelectListItem>();
        }
    }
}
