using EVStationRental.Common.DTOs.ReportDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.Mapper;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IReportServices;

namespace EVStationRental.Services.InternalServices.Services.ReportServices
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAllReportsAsync()
        {
            try
            {
                var reports = await _unitOfWork.ReportRepository.GetAllAsync();

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
                    Message = $"Lỗi khi lấy danh sách báo cáo: {ex.Message}"
                };
            }
        }

        public async Task<IServiceResult> GetReportByIdAsync(Guid reportId)
        {
            try
            {
                var report = await _unitOfWork.ReportRepository.GetByIdAsync(reportId);

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
                    Message = $"Lỗi khi lấy thông tin báo cáo: {ex.Message}"
                };
            }
        }

        public async Task<IServiceResult> GetReportsByAccountIdAsync(Guid accountId)
        {
            try
            {
                // Kiểm tra account có tồn tại không
                var account = await _unitOfWork.AccountRepository.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Tài khoản không tồn tại"
                    };
                }

                var reports = await _unitOfWork.ReportRepository.GetByAccountIdAsync(accountId);

                if (reports == null || !reports.Any())
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm thấy báo cáo nào của tài khoản này"
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
                    Message = $"Lỗi khi lấy danh sách báo cáo theo tài khoản: {ex.Message}"
                };
            }
        }

        public async Task<IServiceResult> CreateReportAsync(CreateReportRequestDTO request)
        {
            try
            {
                // AC3: Kiểm tra vehicle_id có tồn tại không
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Xe không tồn tại trong hệ thống"
                    };
                }

                // Kiểm tra account_id có tồn tại không
                var account = await _unitOfWork.AccountRepository.GetAccountByIdAsync(request.AccountId);
                if (account == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Tài khoản không tồn tại trong hệ thống"
                    };
                }

                if (!account.Isactive)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.FORBIDDEN_ACCESS_CODE,
                        Message = "Tài khoản đã bị vô hiệu hóa"
                    };
                }

                // Lấy role của account
                var role = account.Role?.RoleName;

                if (string.IsNullOrEmpty(role))
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Không xác định được vai trò của tài khoản"
                    };
                }

                // AC4: Nếu là Customer, phải là người đã thuê xe này
                if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                {
                    var hasRented = await _unitOfWork.ReportRepository.HasCustomerRentedVehicleAsync(
                        request.AccountId,
                        request.VehicleId
                    );

                    if (!hasRented)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.FORBIDDEN_ACCESS_CODE,
                            Message = "Bạn chỉ có thể tạo báo cáo cho xe mà bạn đã thuê"
                        };
                    }
                }
                // AC5: Nếu là Staff, kiểm tra xe đã được trả về
                else if (role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                {
                    // Kiểm tra trạng thái xe - Staff chỉ có thể tạo report khi xe đã được trả
                    // hoặc đang ở trạng thái Available, Maintenance
                    var vehicleStatus = vehicle.Status.ToString();

                    if (vehicleStatus == "RENTED")
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.FORBIDDEN_ACCESS_CODE,
                            Message = "Không thể tạo báo cáo khi xe đang được thuê"
                        };
                    }
                }
                else if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.FORBIDDEN_ACCESS_CODE,
                        Message = "Bạn không có quyền tạo báo cáo"
                    };
                }

                // AC2: Tạo report với generated_date, created_at tự động, isActive = true
                var report = request.ToReport();

                // Lưu vào database
                await _unitOfWork.ReportRepository.CreateAsync(report);

                // Lấy lại report với đầy đủ thông tin để trả về
                var createdReport = await _unitOfWork.ReportRepository.GetByIdAsync(report.ReportId);

                if (createdReport == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.FAIL_CREATE_CODE,
                        Message = "Tạo báo cáo thất bại"
                    };
                }

                var response = createdReport.ToCreateReportResponse();

                // AC1: Trả về 201 Created
                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_CREATE_CODE,
                    Message = "Tạo báo cáo thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                // Log chi tiết inner exception để debug
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi tạo báo cáo: {innerMessage}"
                };
            }
        }

        public async Task<IServiceResult> UpdateReportAsync(Guid reportId, UpdateReportRequestDTO request)
        {
            try
            {
                // Lấy report cần update
                var report = await _unitOfWork.ReportRepository.GetByIdAsync(reportId);
                
                if (report == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm thấy báo cáo"
                    };
                }

                // Kiểm tra report có bị inactive không
                if (!report.Isactive)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.FORBIDDEN_ACCESS_CODE,
                        Message = "Báo cáo đã bị khóa, không thể chỉnh sửa"
                    };
                }

                // Bỏ qua validation quyền - sẽ làm sau
                // AC3: Cập nhật chỉ report_type và text, updated_at
                // Giữ nguyên account_id, vehicle_id, created_at
                report.UpdateReportFromDto(request);

                // Lưu vào database
                await _unitOfWork.ReportRepository.UpdateAsync(report);

                // Lấy lại report với đầy đủ thông tin để trả về
                var updatedReport = await _unitOfWork.ReportRepository.GetByIdAsync(reportId);

                if (updatedReport == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.FAIL_UPDATE_CODE,
                        Message = "Cập nhật báo cáo thất bại"
                    };
                }

                var response = updatedReport.ToCreateReportResponse();

                // AC1: Trả về 200 OK
                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_UPDATE_CODE,
                    Message = Const.SUCCESS_UPDATE_MSG,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi cập nhật báo cáo: {innerMessage}"
                };
            }
        }
    }
}
