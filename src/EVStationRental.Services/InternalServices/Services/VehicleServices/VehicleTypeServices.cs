using EVStationRental.Common.DTOs.VehicleTypeDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.Mapper;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.Services.VehicleServices
{
    public class VehicleTypeServices : IVehicleTypeServices
    {
        private readonly IUnitOfWork _unitOfWork;

        public VehicleTypeServices(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> CreateVehicleTypeAsync(CreateVehicleTypeRequestDTO dto)
        {
            var existingVehicleType = (await _unitOfWork.VehicleTypeRepository.GetAllVehicleTypesAsync())
                .FirstOrDefault(vt => vt.TypeName == dto.TypeName);

            if (existingVehicleType != null) 
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_VALIDATION_CODE,
                    Message = "Loại xe đã tồn tại"
                };
            }

            var vehicleType = dto.ToVehicleType();
            var result = await _unitOfWork.VehicleTypeRepository.CreateVehicleTypeAsync(vehicleType);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_CREATE_CODE,
                Message = Const.SUCCESS_CREATE_MSG,
                Data = result.ToViewVehicleTypeDTO()
            };

        }

        public async Task<IServiceResult> GetAllActiveVehicleTypesAsync()
        {
            var vehicleTypes = await _unitOfWork.VehicleTypeRepository.GetAllActiveVehicleTypesAsync();
            var vehicleTypeDtos = vehicleTypes.Select(vt => vt.ToViewVehicleTypeDTO()).ToList();

            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_READ_CODE,
                Message = "Lấy danh sách loại xe đang hoạt động thành công.",
                Data = vehicleTypeDtos
            };
        }

        public async Task<IServiceResult> GetAllVehicleTypesAsync()
        {
            try
            {
                var vehicleTypes = await _unitOfWork.VehicleTypeRepository.GetAllVehicleTypesAsync();
                if (vehicleTypes == null || vehicleTypes.Count == 0)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không có loại xe nào."
                    };
                }

                var vehicleTypeDtos = vehicleTypes.Select(vt => vt.ToViewVehicleTypeDTO()).ToList();

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Lấy danh sách loại xe thành công.",
                    Data = vehicleTypeDtos
                };
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy danh sách loại xe: {ex.Message} {innerMessage}"
                };
            }

        }

        public async Task<IServiceResult> GetVehicleTypeByIdAsync(Guid vehicleTypeId)
        {
            var vehicleType = await _unitOfWork.VehicleTypeRepository.GetVehicleTypeByIdAsync(vehicleTypeId);
            if (vehicleType == null)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy loại xe" };
            var dto = vehicleType.ToViewVehicleTypeDTO();
            return new ServiceResult { StatusCode = Const.SUCCESS_READ_CODE, Message = Const.SUCCESS_READ_MSG, Data = dto };


        }

        public async Task<IServiceResult> SoftDeleteVehicleTypeAsync(Guid vehicleTypeId)
        {
            var success = await  _unitOfWork.VehicleTypeRepository.SoftDeleteVehicleTypeAsync(vehicleTypeId);
            if (!success)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy loại xe hoặc đã bị xóa" };
            return new ServiceResult { StatusCode = Const.SUCCESS_UPDATE_CODE, Message = "Xóa mềm loại xe thành công" };
        }

        public async Task<IServiceResult> HardDeleteVehicleTypeAsync(Guid vehicleTypeId)
        {
            var success = await _unitOfWork.VehicleTypeRepository.HardDeleteVehicleTypeAsync(vehicleTypeId);
            if (!success)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy loại xe" };
            return new ServiceResult { StatusCode = Const.SUCCESS_DELETE_CODE, Message = "Xóa cứng loại xe thành công" };
        }


        public async Task<IServiceResult> UpdateVehicleTypeAsync(Guid vehicleTypeId, UpdateVehicleTypeRequestDTO dto)
        {
            var vehicleType = await _unitOfWork.VehicleTypeRepository.GetVehicleTypeByIdAsync(vehicleTypeId);
            if (vehicleType == null)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy loại xe" };

            dto.MapToVehicleType(vehicleType);

            var updatedVehicleType = await _unitOfWork.VehicleTypeRepository.UpdateVehicleTypeAsync(vehicleType);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_UPDATE_CODE,
                Message = Const.SUCCESS_UPDATE_MSG,
                Data = updatedVehicleType.ToViewVehicleTypeDTO()
            };
        }
    }
}
