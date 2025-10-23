using System;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.OrderDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.Mapper;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;

namespace EVStationRental.Services.InternalServices.Services.OrderServices
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> CreateOrderAsync(Guid customerId, CreateOrderRequestDTO request)
        {
            try
            {
                // Validate time ranges
                if (request.StartTime <= DateTime.Now)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Th?i gian b?t ??u ph?i sau th?i ?i?m hi?n t?i"
                    };
                }

                if (request.EndTime <= request.StartTime)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Th?i gian k?t thúc ph?i sau th?i gian b?t ??u"
                    };
                }

                // Check if vehicle exists and get its model information
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Xe không t?n t?i trong h? th?ng"
                    };
                }

                // Check vehicle availability
                var isAvailable = await _unitOfWork.OrderRepository.IsVehicleAvailableAsync(
                    request.VehicleId, 
                    request.StartTime, 
                    request.EndTime);

                if (!isAvailable)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Xe không kh? d?ng trong kho?ng th?i gian ?ã ch?n"
                    };
                }

                // Get vehicle model to calculate price
                var vehicleModel = await _unitOfWork.VehicleModelRepository.GetVehicleModelByIdAsync(vehicle.ModelId);
                if (vehicleModel == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_EXCEPTION,
                        Message = "Không tìm th?y thông tin m?u xe"
                    };
                }

                // Calculate total hours and price
                var totalHours = (decimal)(request.EndTime - request.StartTime).TotalHours;
                var originalPrice = totalHours * vehicleModel.PricePerHour;
                var totalPrice = originalPrice;
                decimal? discountAmount = null;
                Guid? promotionId = null;

                // Apply promotion if provided
                if (!string.IsNullOrEmpty(request.PromotionCode))
                {
                    var promotion = await _unitOfWork.PromotionRepository.GetByCodeAsync(request.PromotionCode);
                    
                    if (promotion == null)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.WARNING_NO_DATA_CODE,
                            Message = "Mã khuy?n mãi không t?n t?i"
                        };
                    }

                    if (!promotion.Isactive)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.ERROR_VALIDATION_CODE,
                            Message = "Mã khuy?n mãi ?ã h?t h?n ho?c không còn hi?u l?c"
                        };
                    }

                    // Check if promotion is within valid date range
                    var now = DateTime.Now;
                    if (now < promotion.StartDate || now > promotion.EndDate)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.ERROR_VALIDATION_CODE,
                            Message = "Mã khuy?n mãi không trong th?i gian áp d?ng"
                        };
                    }

                    // Apply discount
                    discountAmount = originalPrice * (promotion.DiscountPercentage / 100);
                    totalPrice = originalPrice - discountAmount.Value;
                    promotionId = promotion.PromotionId;
                }

                // Create order
                var order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    CustomerId = customerId,
                    VehicleId = request.VehicleId,
                    OrderDate = DateTime.Now,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    TotalPrice = totalPrice,
                    PromotionId = promotionId,
                    Status = OrderStatus.PENDING,
                    CreatedAt = DateTime.Now,
                    Isactive = true
                };

                var createdOrder = await _unitOfWork.OrderRepository.CreateOrderAsync(order);

                // Load related entities for response
                var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderByIdAsync(createdOrder.OrderId);
                
                var response = orderWithDetails!.ToCreateOrderResponseDTO();
                response.OriginalPrice = originalPrice;
                response.DiscountAmount = discountAmount;

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_CREATE_CODE,
                    Message = "??t xe thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                // Log inner exception details for debugging
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                var stackTrace = ex.InnerException?.StackTrace ?? ex.StackTrace;
                
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"L?i khi t?o ??n ??t xe: {innerMessage}",
                    Data = new { 
                        error = ex.Message, 
                        innerError = ex.InnerException?.Message,
                        stackTrace = stackTrace
                    }
                };
            }
        }

        public async Task<IServiceResult> GetOrderByIdAsync(Guid orderId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm th?y ??n ??t xe"
                    };
                }

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "L?y thông tin ??n ??t xe thành công",
                    Data = order.ToCreateOrderResponseDTO()
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"L?i khi l?y thông tin ??n ??t xe: {ex.Message}"
                };
            }
        }

        public async Task<IServiceResult> GetOrdersByCustomerIdAsync(Guid customerId)
        {
            try
            {
                var orders = await _unitOfWork.OrderRepository.GetOrdersByCustomerIdAsync(customerId);
                
                var orderResponses = orders.Select(o => o.ToCreateOrderResponseDTO()).ToList();

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "L?y danh sách ??n ??t xe thành công",
                    Data = orderResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"L?i khi l?y danh sách ??n ??t xe: {ex.Message}"
                };
            }
        }

        public async Task<IServiceResult> CancelOrderAsync(Guid orderId, Guid customerId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetOrderByIdAsync(orderId);
                
                if (order == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm th?y ??n ??t xe"
                    };
                }

                // Check if order belongs to customer
                if (order.CustomerId != customerId)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.FORBIDDEN_ACCESS_CODE,
                        Message = "B?n không có quy?n h?y ??n ??t xe này"
                    };
                }

                // Only allow canceling pending or confirmed orders
                if (order.Status != OrderStatus.PENDING && order.Status != OrderStatus.CONFIRMED)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Không th? h?y ??n ??t xe ? tr?ng thái hi?n t?i"
                    };
                }

                order.Status = OrderStatus.CANCELED;
                order.UpdatedAt = DateTime.Now;

                await _unitOfWork.OrderRepository.UpdateOrderAsync(order);

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_UPDATE_CODE,
                    Message = "H?y ??n ??t xe thành công"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"L?i khi h?y ??n ??t xe: {ex.Message}"
                };
            }
        }
    }
}
