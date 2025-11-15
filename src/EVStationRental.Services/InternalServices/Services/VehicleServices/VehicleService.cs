using EVStationRental.Common.DTOs.Realtime;
using EVStationRental.Common.DTOs.VehicleDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.Mapper;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.Services.VehicleServices
{
    public class VehicleService : IVehicleService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IRealtimeNotifier _realtimeNotifier;

        public VehicleService(IUnitOfWork unitOfWork, IRealtimeNotifier realtimeNotifier)
        {
            this.unitOfWork = unitOfWork;
            _realtimeNotifier = realtimeNotifier;
        }

        public async Task<IServiceResult> GetAllVehiclesAsync()
        {
            try
            {
                var vehicles = await unitOfWork.VehicleRepository.GetAllVehiclesAsync();
                if (vehicles == null || vehicles.Count == 0)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = Const.WARNING_NO_DATA_MSG
                    };
                }

                var vehicleDtos = vehicles.Select(v => v.ToViewVehicleDTO()).ToList();

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = Const.SUCCESS_READ_MSG,
                    Data = vehicleDtos
                };
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy danh sách xe: {ex.Message} {innerMessage}"
                };
            }
        }

        public async Task<IServiceResult> GetVehicleByIdAsync(Guid id)
        {
            var vehicle = await unitOfWork.VehicleRepository.GetVehicleByIdAsync(id);
            if (vehicle == null)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy xe" };
            var dto = vehicle.ToViewVehicleDTO();
            return new ServiceResult { StatusCode = Const.SUCCESS_READ_CODE, Message = Const.SUCCESS_READ_MSG, Data = dto };
        }

        public async Task<IServiceResult> CreateVehicleAsync(CreateVehicleRequestDTO dto)
        {
            // Kiểm tra model có tồn tại không
            var model = await unitOfWork.VehicleModelRepository.GetVehicleModelByIdAsync(dto.ModelId);
            if (model == null)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_VALIDATION_CODE,
                    Message = "ModelId không hợp lệ"
                };
            }

            // Kiểm tra station có tồn tại không (nếu có truyền lên)
            if (dto.StationId != null)
            {
                var station = await unitOfWork.StationRepository.GetStationByIdAsync(dto.StationId.Value);
                if (station == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "StationId không hợp lệ"
                    };
                }
            }

            // Kiểm tra SerialNumber không bị trùng
            var existingVehicle = (await unitOfWork.VehicleRepository.GetAllVehiclesAsync())
                .FirstOrDefault(v => v.SerialNumber == dto.SerialNumber);
            if (existingVehicle != null)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_VALIDATION_CODE,
                    Message = "SerialNumber đã tồn tại"
                };
            }
            var vehicle = dto.ToVehicle();
            var result = await unitOfWork.VehicleRepository.CreateVehicleAsync(vehicle);
            await NotifyVehicleUpdatedAsync(result ?? vehicle);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_CREATE_CODE,
                Message = Const.SUCCESS_CREATE_MSG,
                Data = result.ToViewVehicleDTO()
            };
        }

        public async Task<IServiceResult> UpdateVehicleAsync(Guid vehicleId, UpdateVehicleRequestDTO dto)
        {
            var vehicle = await unitOfWork.VehicleRepository.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy xe" };

            // Kiểm tra battery
            if (dto.BatteryLevel != null && dto.BatteryLevel <= 0)
                return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "Mức pin phải là số dương" };

            // Kiểm tra StationId nếu có thay đổi
            if (dto.StationId != null)
            {
                var station = await unitOfWork.StationRepository.GetStationByIdAsync(dto.StationId.Value);
                if (station == null)
                    return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "StationId không hợp lệ" };
            }

            // Kiểm tra ModelId nếu có thay đổi
            if (dto.ModelId != null)
            {
                var model = await unitOfWork.VehicleModelRepository.GetVehicleModelByIdAsync(dto.ModelId.Value);
                if (model == null)
                    return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "ModelId không hợp lệ" };
                vehicle.ModelId = dto.ModelId.Value;
            }

            // Map DTO to entity chỉ với các trường có trong model Vehicle, không cập nhật VehicleId
            dto.MapToVehicle(vehicle);

            var updated = await unitOfWork.VehicleRepository.UpdateVehicleAsync(vehicle);
            await NotifyVehicleUpdatedAsync(updated ?? vehicle);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_UPDATE_CODE,
                Message = Const.SUCCESS_UPDATE_MSG,
                Data = updated.ToViewVehicleDTO()
            };
        }

        public async Task<IServiceResult> SoftDeleteVehicleAsync(Guid vehicleId)
        {
            var vehicle = await unitOfWork.VehicleRepository.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy xe hoặc đã bị xóa" };
            }

            var success = await unitOfWork.VehicleRepository.SoftDeleteVehicleAsync(vehicleId);
            if (!success)
            {
                return new ServiceResult { StatusCode = Const.ERROR_EXCEPTION, Message = "Không thể xóa xe" };
            }

            await NotifyVehicleUpdatedAsync(vehicle, isDeleted: true);
            return new ServiceResult { StatusCode = Const.SUCCESS_UPDATE_CODE, Message = "Xóa mềm xe thành công" };
        }

        public async Task<IServiceResult> GetActiveVehiclesAsync()
        {
            var vehicles = await unitOfWork.VehicleRepository.GetActiveVehiclesAsync();
            var vehicleDtos = vehicles.Select(v => v.ToViewVehicleDTO()).ToList();
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_READ_CODE,
                Message = Const.SUCCESS_READ_MSG,
                Data = vehicleDtos
            };
        }

        public async Task<IServiceResult> GetInactiveVehiclesAsync()
        {
            var vehicles = await unitOfWork.VehicleRepository.GetInactiveVehiclesAsync();
            var vehicleDtos = vehicles.Select(v => v.ToViewVehicleDTO()).ToList();
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_READ_CODE,
                Message = Const.SUCCESS_READ_MSG,
                Data = vehicleDtos
            };
        }

        public async Task<IServiceResult> UpdateIsActiveAsync(Guid vehicleId, bool isActive)
        {
            var vehicle = await unitOfWork.VehicleRepository.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy xe" };
            }

            var success = await unitOfWork.VehicleRepository.UpdateIsActiveAsync(vehicleId, isActive);
            if (!success)
            {
                return new ServiceResult { StatusCode = Const.ERROR_EXCEPTION, Message = "Không thể cập nhật trạng thái xe" };
            }

            vehicle.Isactive = isActive;
            await NotifyVehicleUpdatedAsync(vehicle);
            return new ServiceResult { StatusCode = Const.SUCCESS_UPDATE_CODE, Message = "Cập nhật trạng thái xe thành công" };
        }

        public async Task<IServiceResult> GetVehicleWithHighestBatteryByModelAndStationAsync(Guid vehicleModelId, Guid stationId)
        {
            try
            {
                // Kiểm tra model có tồn tại không
                var model = await unitOfWork.VehicleModelRepository.GetVehicleModelByIdAsync(vehicleModelId);
                if (model == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Model không tồn tại trong hệ thống"
                    };
                }

                // Kiểm tra station có tồn tại không
                var station = await unitOfWork.StationRepository.GetStationByIdAsync(stationId);
                if (station == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Trạm không tồn tại trong hệ thống"
                    };
                }

                // Lấy xe có battery level cao nhất
                var vehicle = await unitOfWork.VehicleRepository.GetVehicleWithHighestBatteryByModelAndStationAsync(vehicleModelId, stationId);

                if (vehicle == null)
                {
                    // AC3: Trả về 204 No Content khi không có xe available
                    return new ServiceResult
                    {
                        StatusCode = 204,
                        Message = "No available vehicle found"
                    };
                }

                var vehicleDto = vehicle.ToViewVehicleDTO();

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Lấy thông tin xe thành công",
                    Data = vehicleDto
                };
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy thông tin xe: {innerMessage}"
                };
            }
        }

        private Task NotifyVehicleUpdatedAsync(Vehicle? vehicle, bool isDeleted = false)
        {
            if (vehicle == null)
            {
                return Task.CompletedTask;
            }

            var payload = new VehicleUpdatedPayload
            {
                VehicleId = vehicle.VehicleId,
                SerialNumber = vehicle.SerialNumber,
                Status = vehicle.Status.ToString(),
                IsActive = vehicle.Isactive,
                StationId = vehicle.StationId,
                IsDeleted = isDeleted,
                UpdatedAt = DateTime.UtcNow
            };

            return _realtimeNotifier.NotifyVehicleUpdatedAsync(payload);
        }
    }
}
