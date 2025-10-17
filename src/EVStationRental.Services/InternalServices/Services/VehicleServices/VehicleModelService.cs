using EVStationRental.Common.DTOs.VehicleModelDTOs;
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
    public class VehicleModelService : IVehicleModelService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VehicleModelService(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> CreateVehicleModelAsync(CreateVehicleModelRequestDTO dto)
        {
            // Kiểm tra xem VehicleType có tồn tại không
            var vehicleType = await _unitOfWork.VehicleTypeRepository.GetVehicleTypeByIdAsync(dto.TypeId);
            if (vehicleType == null)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_VALIDATION_CODE,
                    Message = "TypeId không hợp lệ"
                };
            }


            var existingVehicleType = (await _unitOfWork.VehicleModelRepository.GetAllVehicleModelsAsync())
                .FirstOrDefault(vm => vm.Name == dto.Name && vm.Manufacturer == dto.Manufacturer);

            if (existingVehicleType != null)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_VALIDATION_CODE,
                    Message = "Mẫu xe đã tồn tại"
                };
            }

            var vehicleModel = dto.ToVehicleModel();
            var result = await _unitOfWork.VehicleModelRepository.CreateVehicleModelAsync(vehicleModel);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_CREATE_CODE,
                Message = "Mẫu xe mới tạo thành công",
                Data = result.ToViewVehicleModelDTO()
            };

        }

        public async Task<IServiceResult> GetActiveVehicleModelsAsync()
        {
            var vehicleModels = await _unitOfWork.VehicleModelRepository.GetActiveVehicleModelsAsync();
            var vehicleModelDtos = vehicleModels.Select(vm => vm.ToViewVehicleModelDTO()).ToList();

            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_READ_CODE,
                Message = "Lấy danh sách mẫu xe đang hoạt động thành công",
                Data = vehicleModelDtos
            };

        }

        public async Task<IServiceResult> GetAllVehicleModelsAsync()
        {
            try
            {
                var vehicleModel = await _unitOfWork.VehicleModelRepository.GetAllVehicleModelsAsync();
                if (vehicleModel == null || vehicleModel.Count == 0)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không có mẫu xe nào."
                    };
                }

                var vehicleModelDtos = vehicleModel.Select(vt => vt.ToViewVehicleModelDTO()).ToList();
                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Lấy danh sách mẫu xe thành công.",
                    Data = vehicleModelDtos
                };

            }
            catch (Exception ex) 
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy danh sách mẫu xe: {ex.Message} {innerMessage}"
                };
            }

            //catch (Exception ex)
            //{
            //    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
            //    return new ServiceResult
            //    {
            //        StatusCode = Const.ERROR_EXCEPTION,
            //        Message = $"Lỗi khi lấy danh sách loại xe: {ex.Message} {innerMessage}"
            //    };
            //}
        }

        public async Task<IServiceResult?> GetVehicleModelByIdAsync(Guid id)
        {
            var vehicleModel = await _unitOfWork.VehicleModelRepository.GetVehicleModelByIdAsync(id);
            if (vehicleModel == null)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy loại xe" };
            var dto = vehicleModel.ToViewVehicleModelDTO();
            return new ServiceResult { StatusCode = Const.SUCCESS_READ_CODE, Message = "Lấy mẫu xe theo id thành công", Data = dto };
        }

        public async Task<IServiceResult> HardDeleteVehicleModelAsync(Guid id)
        {
            var success = await _unitOfWork.VehicleModelRepository.HardDeleteVehicleModelAsync(id);
            if (!success)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy mẫu xe" };
            return new ServiceResult { StatusCode = Const.SUCCESS_DELETE_CODE, Message = "Xóa cứng mẫu xe thành công" };
        }

        public async Task<IServiceResult> SoftDeleteVehicleModelAsync(Guid id)
        {
            var success = await _unitOfWork.VehicleModelRepository.SoftDeleteVehicleModelAsync(id);
            if (!success)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy mẫu xe hoặc đã bị xóa" };
            return new ServiceResult { StatusCode = Const.SUCCESS_UPDATE_CODE, Message = "Xóa mềm mẫu xe thành công" };
        }

        public async Task<IServiceResult?> UpdateVehicleModelAsync(Guid id, UpdateVehicleModelRequestDTO dto)
        {
            var vehicleModel = await _unitOfWork.VehicleModelRepository.GetVehicleModelByIdAsync(id);
            if (vehicleModel == null)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy mẫu xe" };


            // Kiểm tra TypeId nếu có thay đổi
            if (dto.TypeId != vehicleModel.TypeId)
            {
                var vehicleType = await _unitOfWork.VehicleTypeRepository.GetVehicleTypeByIdAsync(dto.TypeId);
                if (vehicleType == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "TypeId không hợp lệ"
                    };
                }
                vehicleModel.TypeId = dto.TypeId;
            }

            dto.MapToVehicleModel(vehicleModel);

            var updatedVehicleModel = await _unitOfWork.VehicleModelRepository.UpdateVehicleModelAsync(vehicleModel);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_UPDATE_CODE,
                Message = Const.SUCCESS_UPDATE_MSG,
                Data = updatedVehicleModel.ToViewVehicleModelDTO()
            };
        }
    }
}
