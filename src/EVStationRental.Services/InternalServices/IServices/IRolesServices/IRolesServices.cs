using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVStationRental.Services.Base;

namespace EVStationRental.Services.InternalServices.IServices.IRolesServices
{
    public interface IRolesServices
    {
        Task<IServiceResult> GetAllRolesAsync();
        Task<IServiceResult> GetRoleByIdAsync(Guid roleId);
        Task<IServiceResult> GetRoleByNameAsync(string roleName);
    }
}