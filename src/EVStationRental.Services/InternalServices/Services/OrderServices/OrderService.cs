using System;
using System.Linq;
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

        /// <summary>
        /// Bắt đầu sử dụng xe (chuyển status sang ONGOING khi khách nhận xe)
        /// </summary>
        public async Task<IServiceResult> StartOrderAsync(Guid orderId)
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

                // Only allow starting orders that are CONFIRMED
                if (order.Status != OrderStatus.CONFIRMED)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = $"Không thể bắt đầu đơn đặt xe ở trạng thái {order.Status}. Đơn hàng phải ở trạng thái CONFIRMED."
                    };
                }

                // Check if current time is appropriate to start
                if (DateTime.Now < order.StartTime.AddHours(-1)) // Allow 1 hour early
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Chưa đến thời gian bắt đầu sử dụng xe"
                    };
                }

                // Update order status to ONGOING
                order.Status = OrderStatus.ONGOING;
                order.UpdatedAt = DateTime.Now;
                await _unitOfWork.OrderRepository.UpdateOrderAsync(order);

                // Vehicle status should already be RENTED, but we can verify
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleByIdAsync(order.VehicleId);
                if (vehicle != null && vehicle.Status != VehicleStatus.RENTED)
                {
                    vehicle.Status = VehicleStatus.RENTED;
                    vehicle.UpdatedAt = DateTime.Now;
                    await _unitOfWork.VehicleRepository.UpdateVehicleAsync(vehicle);
                }

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_UPDATE_CODE,
                    Message = "Bắt đầu sử dụng xe thành công",
                    Data = new
                    {
                        OrderId = order.OrderId,
                        Status = order.Status.ToString(),
                        StartTime = order.StartTime,
                        EndTime = order.EndTime,
                        VehicleId = order.VehicleId
                    }
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi bắt đầu đơn đặt xe: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Create order with wallet deposit - uses stored procedure
        /// </summary>
        public async Task<IServiceResult> CreateOrderWithWalletDepositAsync(Guid customerId, CreateOrderWithWalletDTO request)
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

                // Check vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Xe không tồn tại trong hệ thống"
                    };
                }

                // Get vehicle model to calculate prices
                var vehicleModel = await _unitOfWork.VehicleModelRepository.GetVehicleModelByIdAsync(vehicle.ModelId);
                if (vehicleModel == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_EXCEPTION,
                        Message = "Không tìm thấy thông tin mẫu xe"
                    };
                }

                // Calculate prices
                var totalHours = (decimal)(request.EndTime - request.StartTime).TotalHours;
                var basePrice = totalHours * vehicleModel.PricePerHour;
                var totalPrice = basePrice;
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

                    if (!promotion.Isactive || DateTime.Now < promotion.StartDate || DateTime.Now > promotion.EndDate)
                    {
                        return new ServiceResult
                        {
                            StatusCode = Const.ERROR_VALIDATION_CODE,
                            Message = "Mã khuyến mãi không còn hiệu lực"
                        };
                    }

                    totalPrice = basePrice * (1 - promotion.DiscountPercentage / 100);
                    promotionId = promotion.PromotionId;
                }

                // Calculate deposit (10% of base price)
                var depositAmount = basePrice * 0.10m;

                // Call stored procedure to create order with deposit
                var orderId = await _unitOfWork.OrderRepository.CreateOrderWithDepositUsingWalletAsync(
                    customerId,
                    request.VehicleId,
                    DateTime.Now,
                    request.StartTime,
                    request.EndTime,
                    basePrice,
                    totalPrice,
                    depositAmount,
                    request.PaymentMethod,
                    promotionId,
                    null  // staffId will be set later when confirmed
                );

                // Get created order with details
                var createdOrder = await _unitOfWork.OrderRepository.GetOrderByIdAsync(orderId);
                if (createdOrder == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_EXCEPTION,
                        Message = "Không thể lấy thông tin đơn hàng sau khi tạo"
                    };
                }

                var response = new CreateOrderWithWalletResponseDTO
                {
                    OrderId = createdOrder.OrderId,
                    OrderCode = createdOrder.OrderCode,
                    OrderDate = createdOrder.OrderDate,
                    StartTime = createdOrder.StartTime,
                    EndTime = createdOrder.EndTime ?? DateTime.MinValue,
                    BasePrice = createdOrder.BasePrice,
                    TotalPrice = createdOrder.TotalPrice,
                    DepositAmount = depositAmount,
                    Status = createdOrder.Status.ToString(),
                    PaymentMethod = request.PaymentMethod,
                    Vehicle = new VehicleInfoDTO
                    {
                        VehicleId = vehicle.VehicleId,
                        LicensePlate = vehicle.SerialNumber,
                        ModelName = vehicleModel.Name
                    },
                    Contract = createdOrder.Contracts.FirstOrDefault() != null 
                        ? new ContractInfoDTO
                        {
                            ContractId = createdOrder.Contracts.First().ContractId,
                            ContractDate = createdOrder.Contracts.First().ContractDate,
                            FileUrl = createdOrder.Contracts.First().FileUrl
                        }
                        : new ContractInfoDTO()
                };

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_CREATE_CODE,
                    Message = "Đặt xe thành công. Vui lòng đến trạm với mã đơn hàng để nhận xe.",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi tạo đơn đặt xe: {ex.Message}",
                    Data = new { error = ex.Message, innerError = ex.InnerException?.Message }
                };
            }
        }

        /// <summary>
        /// Verify order code (for staff to confirm customer pickup)
        /// </summary>
        public async Task<IServiceResult> VerifyOrderCodeAsync(string orderCode)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetOrderByCodeAsync(orderCode);
                
                if (order == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm thấy đơn hàng với mã này"
                    };
                }

                // Check if deposit is paid
                var depositPayment = order.Payments
                    .FirstOrDefault(p => p.PaymentType == PaymentType.DEPOSIT && p.Status == "COMPLETED");

                var response = new VerifyOrderCodeResponseDTO
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.Username,
                    CustomerEmail = order.Customer.Email,
                    CustomerPhone = order.Customer.ContactNumber ?? "",
                    VehicleId = order.VehicleId,
                    VehicleLicensePlate = order.Vehicle.SerialNumber,
                    VehicleModel = order.Vehicle.Model?.Name ?? "",
                    StartTime = order.StartTime,
                    EndTime = order.EndTime ?? DateTime.MinValue,
                    TotalPrice = order.TotalPrice,
                    DepositPaid = depositPayment?.Amount ?? 0,
                    OrderStatus = order.Status.ToString(),
                    IsDepositPaid = depositPayment != null,
                    ContractId = order.Contracts.FirstOrDefault()?.ContractId ?? Guid.Empty,
                    ContractFileUrl = order.Contracts.FirstOrDefault()?.FileUrl ?? ""
                };

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Xác thực mã đơn hàng thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi xác thực mã đơn hàng: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Confirm order by staff and transition to ONGOING status
        /// </summary>
        public async Task<IServiceResult> ConfirmOrderByStaffAsync(Guid orderId, Guid staffId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetOrderByIdAsync(orderId);
                
                if (order == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm thấy đơn hàng"
                    };
                }

                if (order.Status != OrderStatus.CONFIRMED)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = $"Không thể xác nhận đơn hàng với trạng thái {order.Status}"
                    };
                }

                // Check if deposit is paid
                var depositPayment = order.Payments
                    .FirstOrDefault(p => p.PaymentType == PaymentType.DEPOSIT && p.Status == "COMPLETED");

                if (depositPayment == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.ERROR_VALIDATION_CODE,
                        Message = "Khách hàng chưa thanh toán tiền cọc"
                    };
                }

                // Update order status and assign staff
                order.Status = OrderStatus.ONGOING;
                order.StaffId = staffId;
                order.UpdatedAt = DateTime.Now;

                await _unitOfWork.OrderRepository.UpdateOrderAsync(order);

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_UPDATE_CODE,
                    Message = "Xác nhận đơn hàng thành công. Khách hàng có thể nhận xe.",
                    Data = new
                    {
                        OrderId = order.OrderId,
                        OrderCode = order.OrderCode,
                        Status = order.Status.ToString(),
                        StaffId = staffId
                    }
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi xác nhận đơn hàng: {ex.Message}"
                };
            }
        }
    }
}
