using EVStationRental.Common.DTOs.FeedbackDTOs;
using EVStationRental.Common.DTOs.VehicleTypeDTOs;
using EVStationRental.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.IServices.IFeedbackServices
{
    public interface IFeedbackService
    {
        Task<IServiceResult> GetAllFeedbackAsync();
        Task<IServiceResult> GetFeedbackByIdAsync(Guid feedbackId);
        Task<IServiceResult> GetFeedbacksByCustomerIdAsync(Guid customerId);
        Task<IServiceResult> GetFeedbackByOrderIdAsync(Guid orderId);
        Task<IServiceResult> CreateFeedbackAsync(CreateFeedbackRequestDTO dto);

    }
}
