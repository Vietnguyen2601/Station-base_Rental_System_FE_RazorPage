using EVStationRental.Common.DTOs.ReportDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Mapper;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IReportServices;

namespace EVStationRental.Services.InternalServices.Services.ReportServices
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReportRepository _reportRepository;

        public ReportService(IUnitOfWork unitOfWork, IReportRepository reportRepository)
        {
            _unitOfWork = unitOfWork;
            _reportRepository = reportRepository;
        }

        public async Task<IServiceResult> GetAllReportsAsync()
        {
            try
            {
                var reports = await _reportRepository.GetAllAsync();

                if (reports == null || !reports.Any())
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = Const.WARNING_NO_DATA_MSG
                    };
                }

                var reportResponses = reports.Select(r => r.ToViewReportResponse()).ToList();

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = Const.SUCCESS_READ_MSG,
                    Data = reportResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"L?i khi l?y danh sách báo cáo: {ex.Message}"
                };
            }
        }

        public async Task<IServiceResult> GetReportByIdAsync(Guid reportId)
        {
            try
            {
                var report = await _reportRepository.GetByIdAsync(reportId);

                if (report == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = Const.WARNING_NO_DATA_MSG
                    };
                }

                var reportResponse = report.ToViewReportResponse();

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = Const.SUCCESS_READ_MSG,
                    Data = reportResponse
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"L?i khi l?y thông tin báo cáo: {ex.Message}"
                };
            }
        }
    }
}
