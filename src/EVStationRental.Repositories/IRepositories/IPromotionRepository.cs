using System.Collections.Generic;
using System.Threading.Tasks;
using EVStationRental.Repositories.Models;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IPromotionRepository
    {
        Task<List<Promotion>> GetAllPromotionsAsync();
        Task<Promotion?> GetByCodeAsync(string code);
        Task<Promotion> CreatePromotionAsync(Promotion promotion);
        Task<Promotion?> GetByIdAsync(Guid id);
        Task<Promotion> UpdatePromotionAsync(Promotion promotion);
        Task<bool> HasBeenUsedAsync(Guid promotionId);
        Task<bool> UpdateIsActiveAsync(Guid id, bool isActive);
    }
}
