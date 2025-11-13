using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IRolesServices;

namespace EVStationRental.Services.InternalServices.Services.RoleServices
{
    public class RoleServices: IRolesServices
    {
        private readonly IUnitOfWork _unitOfWork;
    
        public RoleServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAllRolesAsync()
        {
            try
            {
                var roles = await _unitOfWork.RoleRepository.GetAllAsync();
                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = Const.SUCCESS_READ_MSG,
                    Data = roles
                };
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy danh sách vai trò: {ex.Message} {innerMessage}"
                };
            }
        }

        public async Task<IServiceResult> GetRoleByIdAsync(Guid roleId)
        {
            try
            {
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(roleId);
                if (role == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Vai trò không tồn tại"
                    };
                }

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = Const.SUCCESS_READ_MSG,
                    Data = role
                };
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy vai trò: {ex.Message} {innerMessage}"
                };
            }
        }

        public async Task<IServiceResult> GetRoleByNameAsync(string roleName)
        {
            try
            {
                var role = await _unitOfWork.RoleRepository.GetRoleByNameAsync(roleName);
                if (role == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Vai trò không tồn tại"
                    };
                }

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = Const.SUCCESS_READ_MSG,
                    Data = role
                };
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy vai trò: {ex.Message} {innerMessage}"
                };
            }
        }
    }
}