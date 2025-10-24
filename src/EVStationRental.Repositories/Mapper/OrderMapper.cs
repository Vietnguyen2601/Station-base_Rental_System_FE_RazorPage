using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Repositories.Models;

namespace EVStationRental.Repositories.Mapper
{
    public static class OrderMapper
    {
        public static CreateOrderResponseDTO ToCreateOrderResponseDTO(this Order order)
        {
            return new CreateOrderResponseDTO
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                VehicleId = order.VehicleId,
                OrderDate = order.OrderDate,
                StartTime = order.StartTime,
                EndTime = order.EndTime ?? DateTime.Now,
                TotalPrice = order.TotalPrice,
                OriginalPrice = order.TotalPrice, // Will be calculated in service
                DiscountAmount = null, // Will be calculated in service
                PromotionCode = order.Promotion?.PromoCode,
                Status = order.Status,
                VehicleModelName = order.Vehicle?.Model?.Name ?? string.Empty,
                PricePerHour = order.Vehicle?.Model?.PricePerHour ?? 0
            };
        }
    }
}
