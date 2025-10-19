using System.Threading.Tasks;
using EVStationRental.Common.DTOs.PromotionDTOs;
using EVStationRental.Services.Base;
using System;

namespace EVStationRental.Services.InternalServices.IServices.IPromotionServices
{
    public interface IPromotionService
    {
        Task<IServiceResult> GetAllPromotionsAsync();
        Task<IServiceResult> CreatePromotionAsync(CreatePromotionRequestDTO dto);
        Task<IServiceResult> UpdatePromotionAsync(Guid promotionId, UpdatePromotionRequestDTO dto);
        Task<IServiceResult> UpdateIsActiveAsync(Guid promotionId, bool isActive);
    }
}
