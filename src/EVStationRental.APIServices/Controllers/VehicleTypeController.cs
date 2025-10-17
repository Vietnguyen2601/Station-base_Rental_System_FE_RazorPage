using EVStationRental.Common.DTOs.VehicleTypeDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.InternalServices.Services.VehicleServices;
using Microsoft.AspNetCore.Mvc;

namespace EVStationRental.APIServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleTypeController : ControllerBase
    {
        private readonly IVehicleTypeServices _vehicleTypeService;

        public VehicleTypeController(IVehicleTypeServices vehicleTypeService)
        {
            _vehicleTypeService = vehicleTypeService ?? throw new ArgumentNullException(nameof(vehicleTypeService));
        }

        /// <summary>
        /// Lấy danh sách tất cả loại xe
        /// </summary>
        /// <returns>Danh sách xe trong hệ thống</returns>
        /// <response code="200">Trả về danh sách loại xe thành công</response>
        /// <response code="404">Không tìm thấy dữ liệu</response>
        /// <response code="500">Lỗi server khi xử lý yêu cầu</response>
        /// 

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<ActionResult<IServiceResult>> GetAllVehicleTypesAsync()
        {
            var result = await _vehicleTypeService.GetAllVehicleTypesAsync();
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
        public async Task<ActionResult<IServiceResult>> GetVehicleTypeByIdAsync(Guid id)
        {
            var result = await _vehicleTypeService.GetVehicleTypeByIdAsync(id);
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

        [HttpPost]
        public async Task<ActionResult<IServiceResult>> CreateVehicleTypeAsync([FromBody] CreateVehicleTypeRequestDTO dto)
        {
            var result = await _vehicleTypeService.CreateVehicleTypeAsync(dto);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<IServiceResult>> UpdateVehicleTypeAsync(Guid id, [FromBody] UpdateVehicleTypeRequestDTO dto)
        {
            var result = await _vehicleTypeService.UpdateVehicleTypeAsync(id, dto);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<IServiceResult>> SoftDeleteVehicleTypeAsync(Guid id)
        {
            var result = await _vehicleTypeService.SoftDeleteVehicleTypeAsync(id);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpDelete("hardDelete/{id}")]
        public async Task<ActionResult<IServiceResult>> HardDeleteVehicleTypeAsync(Guid id)
        {
            var result = await _vehicleTypeService.HardDeleteVehicleTypeAsync(id);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpGet("active")]
        public async Task<ActionResult<IServiceResult>> GetActiveVehicleTypesAsync()
        {
            var result = await _vehicleTypeService.GetAllActiveVehicleTypesAsync();
            return Ok(new { Message = result.Message, Data = result.Data });
        }
    }
}
