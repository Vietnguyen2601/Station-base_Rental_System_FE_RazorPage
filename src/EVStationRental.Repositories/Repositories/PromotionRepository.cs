using System.Collections.Generic;
using System.Threading.Tasks;
using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EVStationRental.Repositories.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly ElectricVehicleDBContext _context;

        public PromotionRepository(ElectricVehicleDBContext context)
        {
            _context = context;
        }

        public async Task<List<Promotion>> GetAllPromotionsAsync()
        {
            return await _context.Set<Promotion>().ToListAsync();
        }

        public async Task<Promotion?> GetByCodeAsync(string code)
        {
            return await _context.Set<Promotion>().FirstOrDefaultAsync(p => p.PromoCode == code);
        }

        public async Task<Promotion> CreatePromotionAsync(Promotion promotion)
        {
            _context.Set<Promotion>().Add(promotion);
            await _context.SaveChangesAsync();
            return promotion;
        }

        public async Task<Promotion?> GetByIdAsync(Guid id)
        {
            return await _context.Set<Promotion>().FindAsync(id);
        }

        public async Task<Promotion> UpdatePromotionAsync(Promotion promotion)
        {
            _context.Set<Promotion>().Update(promotion);
            await _context.SaveChangesAsync();
            return promotion;
        }

        public async Task<bool> HasBeenUsedAsync(Guid promotionId)
        {
            return await _context.Orders.AnyAsync(o => o.PromotionId == promotionId);
        }

        public async Task<bool> UpdateIsActiveAsync(Guid id, bool isActive)
        {
            var promo = await GetByIdAsync(id);
            if (promo == null) return false;
            promo.Isactive = isActive;
            _context.Set<Promotion>().Update(promo);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
