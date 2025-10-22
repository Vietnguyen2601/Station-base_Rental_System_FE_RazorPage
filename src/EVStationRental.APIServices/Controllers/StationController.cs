using EVStationRental.Common.DTOs.StationDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IStationServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;

namespace EVStationRental.APIServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Staff,Admin")]
    public class StationController : ControllerBase
    {
        private readonly IStationService _stationService;

        public StationController(IStationService stationService)
        {
            _stationService = stationService ?? throw new ArgumentNullException(nameof(stationService));
        }

        [HttpPost]
        public async Task<ActionResult<IServiceResult>> CreateStationAsync([FromBody] CreateStationRequestDTO dto)
        {
            var result = await _stationService.CreateStationAsync(dto);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpGet]
        public async Task<ActionResult<IServiceResult>> GetAllStationsAsync()
        {
            var result = await _stationService.GetAllStationsAsync();
            if (result.Data == null)
            {
                return NotFound(new { Message = Const.WARNING_NO_DATA_MSG });
            }
            return Ok(new { Message = Const.SUCCESS_READ_MSG, Data = result.Data });
        }

        [HttpGet("{stationId}/vehicles")]
        public async Task<ActionResult<IServiceResult>> GetVehiclesByStationIdAsync(Guid stationId)
        {
            var result = await _stationService.GetVehiclesByStationIdAsync(stationId);
            if (result.Data == null)
                return NotFound(new { Message = result.Message });
            return Ok(new { Message = result.Message, Data = result.Data });
        }

        [HttpPost("{stationId}/vehicles")]
        public async Task<ActionResult<IServiceResult>> AddVehiclesToStationAsync(Guid stationId, [FromBody] List<Guid> vehicleIds)
        {
            var dto = new AddVehiclesToStationRequestDTO { StationId = stationId, VehicleIds = vehicleIds };
            var result = await _stationService.AddVehiclesToStationAsync(dto);
            return StatusCode(result.StatusCode, new { Message = result.Message });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<IServiceResult>> UpdateStationAsync(Guid id, [FromBody] UpdateStationRequestDTO dto)
        {
            var result = await _stationService.UpdateStationAsync(id, dto);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<IServiceResult>> SoftDeleteStationAsync(Guid id)
        {
            var result = await _stationService.SoftDeleteStationAsync(id);
            return StatusCode((int)result.StatusCode, new { Message = result.Message });
        }

        [HttpGet("active")]
        public async Task<ActionResult<IServiceResult>> GetActiveStationsAsync()
        {
            var result = await _stationService.GetActiveStationsAsync();
            return Ok(new { Message = result.Message, Data = result.Data });
        }

        [HttpGet("inactive")]
        public async Task<ActionResult<IServiceResult>> GetInactiveStationsAsync()
        {
            var result = await _stationService.GetInactiveStationsAsync();
            return Ok(new { Message = result.Message, Data = result.Data });
        }

        [HttpPut("{id}/isactive")]
        public async Task<ActionResult<IServiceResult>> UpdateIsActiveAsync(Guid id, [FromBody] bool isActive)
        {
            var result = await _stationService.UpdateIsActiveAsync(id, isActive);
            return StatusCode((int)result.StatusCode, new { Message = result.Message });
        }

        /// <summary>
        /// L?y danh sách tr?m có xe theo model
        /// </summary>
        /// <param name="vehicleModelId">ID c?a model xe</param>
        /// <returns>Danh sách tr?m có ít nh?t 1 xe thu?c model</returns>
        /// <response code="200">Tr? v? danh sách tr?m thành công</response>
        /// <response code="204">Không có tr?m nào có xe thu?c model này</response>
        /// <response code="400">Model không h?p l?</response>
        /// <response code="500">L?i server</response>
        [HttpGet("by-model/{vehicleModelId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IServiceResult>> GetStationsByVehicleModelAsync(Guid vehicleModelId)
        {
            if (vehicleModelId == Guid.Empty)
            {
                return BadRequest(new
                {
                    StatusCode = Const.ERROR_VALIDATION_CODE,
                    Message = "VehicleModelId là b?t bu?c"
                });
            }

            var result = await _stationService.GetStationsByVehicleModelAsync(vehicleModelId);
            
            // AC3: Tr? v? 204 No Content n?u không có tr?m, có kèm message
            if (result.StatusCode == 204)
            {
                return StatusCode(204, new
                {
                    Message = result.Message
                });
            }

            return StatusCode(result.StatusCode, new 
            { 
                Message = result.Message, 
                Data = result.Data 
            });
        }
    }
}
