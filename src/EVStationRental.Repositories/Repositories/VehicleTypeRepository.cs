using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.Repositories
{
    public class VehicleTypeRepository : IVehicleTypeRepository
    {
        private readonly ElectricVehicleDBContext _context;

        public VehicleTypeRepository(ElectricVehicleDBContext context)
        {
            _context = context;
        }
        public async Task<VehicleType> CreateVehicleTypeAsync(VehicleType vehicleType)
        {
            _context.Set<VehicleType>().Add(vehicleType);
            await _context.SaveChangesAsync();
            return vehicleType;

        }

        public async Task<List<VehicleType>> GetAllActiveVehicleTypesAsync()
        {

            return await _context.Set<VehicleType>().Where(vt => vt.Isactive).ToListAsync();

        }

        public async Task<List<VehicleType>> GetAllVehicleTypesAsync()
        {
            return await _context.Set<VehicleType>().ToListAsync();
        }

        public async Task<VehicleType?> GetVehicleTypeByIdAsync(Guid vehicleTypeId)
        {
            return await _context.Set<VehicleType>().FindAsync(vehicleTypeId);
        }

        public Task<bool> HardDeleteVehicleTypeAsync(Guid vehicleTypeId)
        {
            var vehicleType = _context.Set<VehicleType>().Find(vehicleTypeId);
            if (vehicleType == null) return Task.FromResult(false);
            _context.Set<VehicleType>().Remove(vehicleType);
            _context.SaveChanges();
            return Task.FromResult(true);
        }

        public async Task<bool> SoftDeleteVehicleTypeAsync(Guid vehicleTypeId)
        {
            var vehicleType = await GetVehicleTypeByIdAsync(vehicleTypeId);
            if (vehicleType == null) return false;
            vehicleType.Isactive = false;
            _context.Set<VehicleType>().Update(vehicleType);
            await _context.SaveChangesAsync();
            return true;


            //var vehicle = await GetVehicleByIdAsync(vehicleId);
            //if (vehicle == null) return false;
            //vehicle.Isactive = false;
            //_context.Set<Vehicle>().Update(vehicle);
            //await _context.SaveChangesAsync();
            //return true;
        }

        public async Task<VehicleType?> UpdateVehicleTypeAsync(VehicleType vehicleType)
        {
            _context.Set<VehicleType>().Update(vehicleType);
            await _context.SaveChangesAsync();
            return vehicleType;
        }
    }
}
