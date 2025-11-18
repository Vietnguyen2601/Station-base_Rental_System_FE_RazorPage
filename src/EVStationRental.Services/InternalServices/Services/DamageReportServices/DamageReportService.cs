using EVStationRental.Common.DTOs.DamageReportDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Mapper;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IDamageReportServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.Services.DamageReportServices
{
    public class DamageReportService : IDamageReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DamageReportService(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> CreateDamageReportAsync(CreateDamageReportRequestDTO dto)
        {
            var exsitingDamageReport = (await _unitOfWork.DamageReportRepository.GetAllDamageReportsAsync())
                .FirstOrDefault(da => da.OrderId == dto.OrderId);

            if (exsitingDamageReport != null)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_VALIDATION_CODE,
                    Message = "Damage Report đã tồn tại"
                };
            }

            var damageReport = dto.ToDamageReport();
            var result = await _unitOfWork.DamageReportRepository.CreateDamageReportAsync(damageReport);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_CREATE_CODE,
                Message = Const.SUCCESS_CREATE_MSG,
                Data = result.ToViewDamageReportDTO()
            };

        }

        public async Task<IServiceResult> GetAllDamageReports()
        {
            var damageReports = await _unitOfWork.DamageReportRepository.GetAllDamageReportsAsync();
            var damageReportDtos = damageReports.Select(vt => vt.ToViewDamageReportDTO()).ToList();

            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_READ_CODE,
                Message = "Lấy danh sách Damage report  thành công.",
                Data = damageReportDtos
            };
        }

        public async Task<IServiceResult> GetDamageReportByIdAsync(Guid damageReportId)
        {
            var damageReport = await _unitOfWork.DamageReportRepository.GetDamageReportByDamageIdAsync(damageReportId);
            if (damageReport == null)
                return new ServiceResult
                {
                    StatusCode = Const.WARNING_NO_DATA_CODE,
                    Message = "Không tìm thấy damage report"
                };
            var dto = damageReport.ToViewDamageReportDTO();
            return new ServiceResult { StatusCode = Const.SUCCESS_READ_CODE, Message = Const.SUCCESS_READ_MSG, Data = dto };
        }

        public async Task<IServiceResult> GetDamageReportByOrderIdAsync(Guid orderId)
        {
            var damageReport = await _unitOfWork.DamageReportRepository.GetDamageReportByOrderIdAsync(orderId);
            if (damageReport == null)
                return new ServiceResult
                {
                    StatusCode = Const.WARNING_NO_DATA_CODE,
                    Message = "Không tìm thấy damage report"
                };
            var dto = damageReport.ToViewDamageReportDTO();
            return new ServiceResult { StatusCode = Const.SUCCESS_READ_CODE, Message = Const.SUCCESS_READ_MSG, Data = dto };
        }

        public async Task<IServiceResult> GetDamageReportsByVehicleIdAsync(Guid vehicleId)
        {
            var damageReports = await _unitOfWork.DamageReportRepository.GetDamageReportsByVehicleIdAsync(vehicleId);
            var damageReportDtos = damageReports.Select(vt => vt.ToViewDamageReportDTO()).ToList();

            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_READ_CODE,
                Message = "Lấy danh sách Damage report  thành công.",
                Data = damageReportDtos
            };
        }

        public async Task<IServiceResult> HardDeleteDamageReportAsync(Guid DamageReportId)
        {
            var success = await _unitOfWork.DamageReportRepository.HardDeleteDamageReportAsync(DamageReportId);
            if (!success)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy damage report hoặc đã bị xóa" };
            return new ServiceResult { StatusCode = Const.SUCCESS_UPDATE_CODE, Message = "Xóa mềm damage report thành công" };
        }

        public async Task<IServiceResult> SoftDeleteDamageReportAsync(Guid DamageReportId)
        {
            var success = await _unitOfWork.DamageReportRepository.SoftDeleteDamageReportAsync(DamageReportId);
            if (!success)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy damage report hoặc đã bị xóa" };
            return new ServiceResult { StatusCode = Const.SUCCESS_UPDATE_CODE, Message = "Xóa mềm damage report thành công" };
        }

        public async Task<IServiceResult> UpdateDamageReportAsync(Guid DamageReportId, UpdateDamageReportRequestDTO dto)
        {
            var damageReport = await _unitOfWork.DamageReportRepository.GetDamageReportByDamageIdAsync(DamageReportId);
            if (damageReport == null)
                return new ServiceResult
                {
                    StatusCode = Const.WARNING_NO_DATA_CODE,
                    Message = "Không tìm thấy Damage Report",
                };

            dto.MapToDamageReportModel(damageReport);

            var updateDamageReport = await _unitOfWork.DamageReportRepository.UpdateDamageReportAsync(damageReport);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_UPDATE_CODE,
                Message = Const.SUCCESS_UPDATE_MSG,
                Data = updateDamageReport.ToViewDamageReportDTO()
            };

        }
    }
}
