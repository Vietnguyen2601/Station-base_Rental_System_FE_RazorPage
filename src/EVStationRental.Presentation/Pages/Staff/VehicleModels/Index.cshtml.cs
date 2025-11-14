using EVStationRental.Common.DTOs.VehicleModelDTOs;
using EVStationRental.Common.DTOs.VehicleTypeDTOs;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EVStationRental.Presentation.Pages.Staff.VehicleModels;

[Authorize(Roles = "Staff")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IVehicleModelService _vehicleModelService;
    private readonly IVehicleTypeServices _vehicleTypeService;

    public IndexModel(ILogger<IndexModel> logger, IVehicleModelService vehicleModelService, IVehicleTypeServices vehicleTypeService)
    {
        _logger = logger;
        _vehicleModelService = vehicleModelService;
        _vehicleTypeService = vehicleTypeService;
    }

    public List<ViewVehicleModelResponseDTO> VehicleModels { get; set; } = new();
    public List<SelectListItem> VehicleTypes { get; set; } = new();

    [BindProperty]
    public CreateVehicleModelRequestDTO CreateVehicleModelDto { get; set; } = new();

    [BindProperty]
    public UpdateVehicleModelRequestDTO UpdateVehicleModelDto { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty]
    public Guid VehicleModelIdToUpdate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadVehicleTypesAsync();
        await LoadVehicleModelsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Dữ liệu không hợp lệ";
            await LoadVehicleTypesAsync();
            await LoadVehicleModelsAsync();
            return Page();
        }

        try
        {
            var result = await _vehicleModelService.CreateVehicleModelAsync(CreateVehicleModelDto);

            if (result?.StatusCode == 200)
            {
                TempData["Ok"] = "Thêm mẫu xe thành công";
                return RedirectToPage();
            }

            TempData["Err"] = result?.Message ?? "Không thể thêm mẫu xe";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle model");
            TempData["Err"] = "Lỗi khi thêm mẫu xe";
        }

        await LoadVehicleTypesAsync();
        await LoadVehicleModelsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (!ModelState.IsValid || VehicleModelIdToUpdate == Guid.Empty)
        {
            TempData["Err"] = "Dữ liệu không hợp lệ";
            await LoadVehicleTypesAsync();
            await LoadVehicleModelsAsync();
            return Page();
        }

        try
        {
            var result = await _vehicleModelService.UpdateVehicleModelAsync(VehicleModelIdToUpdate, UpdateVehicleModelDto);

            if (result?.StatusCode == 200)
            {
                TempData["Ok"] = "Cập nhật mẫu xe thành công";
                return RedirectToPage();
            }

            TempData["Err"] = result?.Message ?? "Không thể cập nhật mẫu xe";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle model");
            TempData["Err"] = "Lỗi khi cập nhật mẫu xe";
        }

        await LoadVehicleTypesAsync();
        await LoadVehicleModelsAsync();
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
            var result = await _vehicleModelService.SoftDeleteVehicleModelAsync(id);

            if (result?.StatusCode == 200)
            {
                TempData["Ok"] = "Xóa mẫu xe thành công";
            }
            else
            {
                TempData["Err"] = result?.Message ?? "Không thể xóa mẫu xe";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle model");
            TempData["Err"] = "Lỗi khi xóa mẫu xe";
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
                var models = result.Data as List<ViewVehicleModelResponseDTO>
                    ?? new List<ViewVehicleModelResponseDTO>();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    models = models.Where(m =>
                        m.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        m.Manufacturer.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (m.Specs != null && m.Specs.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                VehicleModels = models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicle models");
            VehicleModels = new List<ViewVehicleModelResponseDTO>();
        }
    }

    private async Task LoadVehicleTypesAsync()
    {
        try
        {
            var result = await _vehicleTypeService.GetAllVehicleTypesAsync();

            if (result?.StatusCode == 200 && result.Data != null)
            {
                var types = result.Data as List<ViewVehicleTypeResponse> ?? new List<ViewVehicleTypeResponse>();
                VehicleTypes = types.Select(t => new SelectListItem
                {
                    Value = t.VehicleTypeId.ToString(),
                    Text = t.TypeName
                }).ToList();
            }
            else
            {
                VehicleTypes = new List<SelectListItem>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicle types");
            VehicleTypes = new List<SelectListItem>();
        }
    }
}
