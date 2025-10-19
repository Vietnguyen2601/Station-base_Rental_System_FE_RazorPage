using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IPromotionServices;
using Microsoft.AspNetCore.Mvc;
using EVStationRental.Common.DTOs.PromotionDTOs;

namespace EVStationRental.APIServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<ActionResult<IServiceResult>> GetAllPromotions()
        {
            var result = await _promotionService.GetAllPromotionsAsync();
            if (result.Data == null)
                return NotFound(new { Message = Const.WARNING_NO_DATA_MSG });
            return Ok(new { Message = Const.SUCCESS_READ_MSG, Data = result.Data });
        }

        [HttpPost]
        public async Task<ActionResult<IServiceResult>> CreatePromotion([FromBody] CreatePromotionRequestDTO dto)
        {
            var result = await _promotionService.CreatePromotionAsync(dto);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<IServiceResult>> UpdatePromotion([FromRoute] Guid id, [FromBody] UpdatePromotionRequestDTO dto)
        {
            var result = await _promotionService.UpdatePromotionAsync(id, dto);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }

        [HttpPut("{id}/isactive")]
        public async Task<ActionResult<IServiceResult>> UpdateIsActiveAsync(Guid id, [FromBody] bool isActive)
        {
            var result = await _promotionService.UpdateIsActiveAsync(id, isActive);
            return StatusCode((int)result.StatusCode, new { Message = result.Message, Data = result.Data });
        }
    }
}
