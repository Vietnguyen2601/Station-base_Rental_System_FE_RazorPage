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
                        Message = "Thời gian bắt đầu phải sau thời điểm hiện tại"
                    };
                }

                if (request.EndTime <= request.StartTime)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Thời gian kết thúc phải sau thời gian bắt đầu"
                    };
                }

                // Check if vehicle exists and get its model information
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Xe không tồn tại trong hệ thống"
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
                        Message = "Xe không khả dụng trong khoảng thời gian đã chọn"
                    };
                }

                // Get vehicle model to calculate price
                var vehicleModel = await _unitOfWork.VehicleModelRepository.GetVehicleModelByIdAsync(vehicle.ModelId);
                if (vehicleModel == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_EXCEPTION,
                        Message = "Không tìm thấy thông tin mẫu xe"
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
                            Message = "Mã khuyến mãi không tồn tại"
                        };
                    }

                    if (!promotion.Isactive)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.ERROR_VALIDATION_CODE,
                            Message = "Mã khuyến mãi đã hết hạn hoặc không còn hiệu lực"
                        };
                    }

                    // Check if promotion is within valid date range
                    var now = DateTime.Now;
                    if (now < promotion.StartDate || now > promotion.EndDate)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.ERROR_VALIDATION_CODE,
                            Message = "Mã khuyến mãi không trong thời gian áp dụng"
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

                // Update vehicle status to RENTED after successful order creation
                vehicle.Status = VehicleStatus.RENTED;
                vehicle.UpdatedAt = DateTime.Now;
                await _unitOfWork.VehicleRepository.UpdateVehicleAsync(vehicle);

                // Load related entities for response
                var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderByIdAsync(createdOrder.OrderId);
                
                var response = orderWithDetails!.ToCreateOrderResponseDTO();
                response.OriginalPrice = originalPrice;
                response.DiscountAmount = discountAmount;

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_CREATE_CODE,
                    Message = "Đặt xe thành công",
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
                    Message = $"Lỗi khi tạo đơn đặt xe: {innerMessage}",
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
                        Message = "Không tìm thấy đơn đặt xe"
                    };
                }

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Lấy thông tin đơn đặt xe thành công",
                    Data = order.ToCreateOrderResponseDTO()
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy thông tin đơn đặt xe: {ex.Message}"
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
                    Message = "Lấy danh sách đơn đặt xe thành công",
                    Data = orderResponses
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy danh sách đơn đặt xe: {ex.Message}"
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
                        Message = "Không tìm thấy đơn đặt xe"
                    };
                }

                // Check if order belongs to customer
                if (order.CustomerId != customerId)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.FORBIDDEN_ACCESS_CODE,
                        Message = "Bạn không có quyền hủy đơn đặt xe này"
                    };
                }

                // Only allow canceling pending or confirmed orders
                if (order.Status != OrderStatus.PENDING && order.Status != OrderStatus.CONFIRMED)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Không thể hủy đơn đặt xe ở trạng thái hiện tại"
                    };
                }

                // Update order status to CANCELED
                order.Status = OrderStatus.CANCELED;
                order.UpdatedAt = DateTime.Now;
                await _unitOfWork.OrderRepository.UpdateOrderAsync(order);

                // Update vehicle status back to AVAILABLE
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleByIdAsync(order.VehicleId);
                if (vehicle != null)
                {
                    vehicle.Status = VehicleStatus.AVAILABLE;
                    vehicle.UpdatedAt = DateTime.Now;
                    await _unitOfWork.VehicleRepository.UpdateVehicleAsync(vehicle);
                }

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_UPDATE_CODE,
                    Message = "Hủy đơn đặt xe thành công"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi hủy đơn đặt xe: {ex.Message}"
                };
            }
        }

        public async Task<IServiceResult> GetAllOrdersAsync()
        {
            try
            {
                var orders = await _unitOfWork.OrderRepository.GetAllOrdersAsync();
                if (orders == null || orders.Count == 0)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không có đơn đặt xe nào."
                    };
                }

                var orderDtos = orders.Select(o => o.ToViewOrderDTO()).ToList();

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Lấy danh sách đơn đặt xe thành công.",
                    Data = orderDtos
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy danh sách đơn đặt xe: {ex.Message}"
                };
            }
        }
    }
}
