using EVStationRental.Common.DTOs.VehicleModelDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using Microsoft.AspNetCore.Mvc;

namespace EVStationRental.APIServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleModelController : ControllerBase
    {
        private readonly IVehicleModelService _vehicleModelService;

        public VehicleModelController(IVehicleModelService vehicleModelService)
        {
            _vehicleModelService = vehicleModelService ?? throw new ArgumentNullException(nameof(vehicleModelService));
        }

        /// <summary>
        /// Lấy danh sách tất cả mẫu xe
        /// </summary>
        /// <returns>Danh sách xe trong hệ thống</returns>
        /// <response code="200">Trả về danh sách mẫu xe thành công</response>
        /// <response code="404">Không tìm thấy dữ liệu</response>
        /// <response code="500">Lỗi server khi xử lý yêu cầu</response>
        /// 
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IServiceResult>> GetAllVehicleModelsAsync()
        {
            var result = await _vehicleModelService.GetAllVehicleModelsAsync();
            if (result.Data == null)
            {
                return NotFound(new
                {
                    Message = Const.WARNING_NO_DATA_MSG
                });
            }
            return Ok(new
            {
                Message = Const.SUCCESS_READ_MSG,
                Data = result.Data
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IServiceResult>> GetVehicleModelByIdAsync(Guid id)
        {
            var result = await _vehicleModelService.GetVehicleModelByIdAsync(id);
            if (result.Data == null)
                return NotFound(new { Message = result.Message });
            return Ok(new { Message = result.Message, Data = result.Data });
        }

        [HttpPost]
        public async Task<ActionResult<IServiceResult>> CreateVehicleModelAsync([FromBody] CreateVehicleModelRequestDTO dto)
        {
            var result = await _vehicleModelService.CreateVehicleModelAsync(dto);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });

        }

        [HttpPut("{id}")]
        public async Task<ActionResult<IServiceResult>> UpdateVehicleModelAsync(Guid id, [FromBody] UpdateVehicleModelRequestDTO dto)
        {
            var result = await _vehicleModelService.UpdateVehicleModelAsync(id, dto);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<IServiceResult>> SoftDeleteVehicleModelAsync(Guid id)
        {
            var result = await _vehicleModelService.SoftDeleteVehicleModelAsync(id);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpDelete("hardDelete/{id}")]
        public async Task<ActionResult<IServiceResult>> HardDeleteVehicleModelAsync(Guid id)
        {
            var result = await _vehicleModelService.HardDeleteVehicleModelAsync(id);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpGet("active")]
        public async Task<ActionResult<IServiceResult>> GetActiveVehicleModelsAsync()
        {
            var result = await _vehicleModelService.GetActiveVehicleModelsAsync();
            return Ok(new { Message = result.Message, Data = result.Data });
        }

    }

}
