using EVStationRental.Common.DTOs.PromotionDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.Mapper;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IPromotionServices;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace EVStationRental.Services.InternalServices.Services.PromotionServices
{
    public class PromotionService : IPromotionService
    {
        private readonly IUnitOfWork unitOfWork;

        public PromotionService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAllPromotionsAsync()
        {
            var promotions = await unitOfWork.PromotionRepository.GetAllPromotionsAsync();
            if (promotions == null || promotions.Count == 0)
            {
                return new ServiceResult
                {
                    StatusCode = Const.WARNING_NO_DATA_CODE,
                    Message = Const.WARNING_NO_DATA_MSG
                };
            }
            var data = promotions.Select(p => p.ToViewPromotionDTO()).ToList();
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_READ_CODE,
                Message = Const.SUCCESS_READ_MSG,
                Data = data
            };
        }

        public async Task<IServiceResult> CreatePromotionAsync(CreatePromotionRequestDTO dto)
        {
            // Validate code unique
            var existed = await unitOfWork.PromotionRepository.GetByCodeAsync(dto.PromoCode);
            if (existed != null)
            {
                return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "Promo code đã tồn tại" };
            }
            // Validate percentage and date range
            if (dto.DiscountPercentage < 1 || dto.DiscountPercentage > 100)
            {
                return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "Discount percenttage phải từ 1 đến 100" };
            }
            if (dto.EndDate <= dto.StartDate)
            {
                return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "End Date phải lớn hơn Start Date" };
            }

            var entity = dto.ToPromotion();
            var created = await unitOfWork.PromotionRepository.CreatePromotionAsync(entity);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_CREATE_CODE,
                Message = Const.SUCCESS_CREATE_MSG,
                Data = created.ToViewPromotionDTO()
            };
        }

        public async Task<IServiceResult> UpdatePromotionAsync(Guid promotionId, UpdatePromotionRequestDTO dto)
        {
            var entity = await unitOfWork.PromotionRepository.GetByIdAsync(promotionId);
            if (entity == null)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy promotion" };

            // Nếu promotion đã từng được sử dụng, không cho sửa promo_code
            var used = await unitOfWork.PromotionRepository.HasBeenUsedAsync(promotionId);
            if (used && !string.Equals(entity.PromoCode, dto.PromoCode, StringComparison.OrdinalIgnoreCase))
            {
                return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "Promotion đã được sử dụng, không thể đổi promo_code" };
            }

            // Validate unique promo code nếu đổi mã
            if (!string.Equals(entity.PromoCode, dto.PromoCode, StringComparison.OrdinalIgnoreCase))
            {
                var existed = await unitOfWork.PromotionRepository.GetByCodeAsync(dto.PromoCode);
                if (existed != null)
                    return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "Promo code đã tồn tại" };
            }

            // Validate ngày
            if (dto.DiscountPercentage < 1 || dto.DiscountPercentage > 100)
                return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "discount_percentage phải từ 1 đến 100" };
            if (dto.EndDate <= dto.StartDate)
                return new ServiceResult { StatusCode = Const.ERROR_VALIDATION_CODE, Message = "end_date phải lớn hơn start_date" };

            dto.MapToPromotion(entity);

            // Cảnh báo nếu end_date < ngày hiện tại
            if (entity.EndDate.Date < DateTime.UtcNow.Date)
            {
                var updatedWarn = await unitOfWork.PromotionRepository.UpdatePromotionAsync(entity);
                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_UPDATE_CODE,
                    Message = "Cập nhật thành công, lưu ý: end_date nhỏ hơn ngày hiện tại",
                    Data = updatedWarn.ToViewPromotionDTO()
                };
            }

            var updated = await unitOfWork.PromotionRepository.UpdatePromotionAsync(entity);
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_UPDATE_CODE,
                Message = Const.SUCCESS_UPDATE_MSG,
                Data = updated.ToViewPromotionDTO()
            };
        }

        public async Task<IServiceResult> UpdateIsActiveAsync(Guid promotionId, bool isActive)
        {
            var ok = await unitOfWork.PromotionRepository.UpdateIsActiveAsync(promotionId, isActive);
            if (!ok)
                return new ServiceResult { StatusCode = Const.WARNING_NO_DATA_CODE, Message = "Không tìm thấy promotion" };

            // trả về danh sách đã cập nhật
            var promotions = await unitOfWork.PromotionRepository.GetAllPromotionsAsync();
            var data = promotions.Select(p => p.ToViewPromotionDTO()).ToList();
            return new ServiceResult
            {
                StatusCode = Const.SUCCESS_UPDATE_CODE,
                Message = Const.SUCCESS_UPDATE_MSG,
                Data = data
            };
        }
    }
}
