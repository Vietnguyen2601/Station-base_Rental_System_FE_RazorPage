using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EVStationRental.Repositories.Repositories
{
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
        private readonly ElectricVehicleDContext _context;

        public VehicleRepository(ElectricVehicleDContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Vehicle>> GetAllVehiclesAsync()
        {
            return await _context.Set<Vehicle>()
                .Include(v => v.Model)
                    .ThenInclude(m => m.Type)
                .Include(v => v.Station)
                .Include(v => v.Orders)
                .ToListAsync();
        }

        public async Task<Vehicle?> GetVehicleByIdAsync(Guid vehicleId)
        {
            return await _context.Set<Vehicle>()
                .Include(v => v.Model)
                    .ThenInclude(m => m.Type)
                .Include(v => v.Station)
                .Include(v => v.Orders)
                .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);
        }

        public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
        {
            _context.Set<Vehicle>().Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task<Vehicle?> UpdateVehicleAsync(Vehicle vehicle)
        {
            // Use AsTracking to ensure changes are tracked
            var existingVehicle = await _context.Vehicles
                .AsTracking()
                .FirstOrDefaultAsync(v => v.VehicleId == vehicle.VehicleId);
                
            if (existingVehicle == null)
            {
                return null;
            }

            // Update properties
            existingVehicle.SerialNumber = vehicle.SerialNumber;
            existingVehicle.ModelId = vehicle.ModelId;
            existingVehicle.StationId = vehicle.StationId;
            existingVehicle.Color = vehicle.Color;
            existingVehicle.BatteryLevel = vehicle.BatteryLevel;
            existingVehicle.BatteryCapacity = vehicle.BatteryCapacity;
            existingVehicle.Range = vehicle.Range;
            existingVehicle.LastMaintenance = vehicle.LastMaintenance;
            existingVehicle.Status = vehicle.Status;
            existingVehicle.Img = vehicle.Img;
            existingVehicle.Isactive = vehicle.Isactive;
            existingVehicle.UpdatedAt = vehicle.UpdatedAt;

            await _context.SaveChangesAsync();
            return existingVehicle;
        }

        public async Task<bool> SoftDeleteVehicleAsync(Guid vehicleId)
        {
            var vehicle = await GetVehicleByIdAsync(vehicleId);
            if (vehicle == null) return false;
            vehicle.Isactive = false;
            _context.Set<Vehicle>().Update(vehicle);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Vehicle>> GetActiveVehiclesAsync()
        {
            return await _context.Set<Vehicle>().Where(v => v.Isactive).ToListAsync();
        }

        public async Task<List<Vehicle>> GetInactiveVehiclesAsync()
        {
            return await _context.Set<Vehicle>().Where(v => !v.Isactive).ToListAsync();
        }

        public async Task<bool> UpdateIsActiveAsync(Guid vehicleId, bool isActive)
        {
            var vehicle = await GetVehicleByIdAsync(vehicleId);
            if (vehicle == null) return false;
            vehicle.Isactive = isActive;
            _context.Set<Vehicle>().Update(vehicle);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Vehicle?> GetVehicleWithHighestBatteryByModelAndStationAsync(Guid vehicleModelId, Guid stationId)
        {
            return await _context.Set<Vehicle>()
                .Include(v => v.Model)
                    .ThenInclude(m => m.Type)
                .Include(v => v.Station)
                .Where(v => v.ModelId == vehicleModelId 
                         && v.StationId == stationId 
                         && v.Isactive == true 
                         && v.Status == Common.Enums.EnumModel.VehicleStatus.AVAILABLE)
                .OrderByDescending(v => v.BatteryLevel)
                .ThenByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
