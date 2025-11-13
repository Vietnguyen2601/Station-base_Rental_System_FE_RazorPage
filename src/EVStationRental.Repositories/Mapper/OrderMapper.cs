using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.DTOs.VehicleModelDTOs;
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
                OrderCode = order.OrderCode,
                CustomerId = order.CustomerId,
                VehicleId = order.VehicleId,
                OrderDate = order.OrderDate,
                StartTime = order.StartTime,
                EndTime = order.EndTime ?? DateTime.Now,
                ReturnTime = order.ReturnTime,
                BasePrice = order.BasePrice,
                Status = order.Status,
                VehicleModelName = order.Vehicle?.Model?.Name ?? string.Empty,
                PricePerHour = order.Vehicle?.Model?.PricePerHour ?? 0
            };
        }

        public static ViewOrderResponseDTO ToViewOrderDTO(this Order order)
        {
            return new ViewOrderResponseDTO
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                CustomerId = order.CustomerId,
                VehicleId = order.VehicleId,
                OrderDate = order.OrderDate,
                StartTime = order.StartTime,
                EndTime = order.EndTime,
                ReturnTime = order.ReturnTime,
                BasePrice = order.BasePrice,
                TotalPrice = order.TotalPrice,
                StationName = order.Vehicle?.Station?.Name ?? string.Empty,
                StationAddress = order.Vehicle?.Station?.Address ?? string.Empty,
                PromotionId = order.PromotionId,
                StaffId = order.StaffId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Isactive = order.Isactive,
                Status = order.Status
            };
        }
    }
}
