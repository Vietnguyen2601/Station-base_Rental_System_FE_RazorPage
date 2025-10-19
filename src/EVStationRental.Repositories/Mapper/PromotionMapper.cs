using EVStationRental.Common.DTOs.PromotionDTOs;
using EVStationRental.Repositories.Models;
using System;

namespace EVStationRental.Repositories.Mapper
{
    public static class PromotionMapper
    {
        public static ViewPromotionResponse ToViewPromotionDTO(this Promotion promotion)
        {
            return new ViewPromotionResponse
            {
                PromoCode = promotion.PromoCode,
                DiscountPercentage = promotion.DiscountPercentage,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.Isactive
            };
        }

        public static Promotion ToPromotion(this CreatePromotionRequestDTO dto)
        {
            return new Promotion
            {
                PromotionId = Guid.NewGuid(),
                PromoCode = dto.PromoCode,
                DiscountPercentage = dto.DiscountPercentage,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedAt = DateTime.Now,
                Isactive = true
            };
        }

        public static void MapToPromotion(this UpdatePromotionRequestDTO dto, Promotion entity)
        {
            entity.PromoCode = dto.PromoCode;
            entity.DiscountPercentage = dto.DiscountPercentage;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.UpdatedAt = DateTime.Now;
        }
    }
}
