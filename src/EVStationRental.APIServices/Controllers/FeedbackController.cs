using EVStationRental.Common.DTOs.FeedbackDTOs;
using EVStationRental.Services.InternalServices.IServices.IFeedbackServices;
using Microsoft.AspNetCore.Mvc;

namespace EVStationRental.APIServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeedbacks()
        {
            var result = await _feedbackService.GetAllFeedbackAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{feedbackId}")]
        public async Task<IActionResult> GetFeedbackById(Guid feedbackId)
        {
            var result = await _feedbackService.GetFeedbackByIdAsync(feedbackId);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetFeedbacksByCustomerId(Guid customerId)
        {
            var result = await _feedbackService.GetFeedbacksByCustomerIdAsync(customerId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetFeedbackByOrderId(Guid orderId)
        {
            var result = await _feedbackService.GetFeedbackByOrderIdAsync(orderId);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequestDTO dto)
        {
            var result = await _feedbackService.CreateFeedbackAsync(dto);
            return StatusCode(result.StatusCode, result);
        }
    }
}
