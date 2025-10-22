using EVStationRental.Common.DTOs.ReportDTOs;
using EVStationRental.Services.InternalServices.IServices.IReportServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVStationRental.APIServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Lấy tất cả báo cáo
        /// </summary>
        /// <returns>Danh sách báo cáo</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllReports()
        {
            var result = await _reportService.GetAllReportsAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy báo cáo theo ID
        /// </summary>
        /// <param name="id">ID của báo cáo</param>
        /// <returns>Thông tin báo cáo</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(Guid id)
        {
            var result = await _reportService.GetReportByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy tất cả báo cáo của một tài khoản
        /// </summary>
        /// <param name="accountId">ID của tài khoản</param>
        /// <returns>Danh sách báo cáo của tài khoản</returns>
        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetReportsByAccountId(Guid accountId)
        {
            var result = await _reportService.GetReportsByAccountIdAsync(accountId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo báo cáo mới
        /// </summary>
        /// <param name="request">Thông tin báo cáo</param>
        /// <returns>Báo cáo vừa được tạo</returns>
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _reportService.CreateReportAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật báo cáo
        /// </summary>
        /// <param name="id">ID của báo cáo cần cập nhật</param>
        /// <param name="request">Thông tin cập nhật (report_type, text)</param>
        /// <returns>Báo cáo sau khi cập nhật</returns>
        /// <response code="200">Cập nhật báo cáo thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="403">Báo cáo bị khóa</response>
        /// <response code="404">Không tìm thấy báo cáo</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateReportRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _reportService.UpdateReportAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }
    }
} 