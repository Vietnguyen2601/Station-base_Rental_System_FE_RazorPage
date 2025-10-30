using EVStationRental.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IFeedbackRepository
    {
        // Define methods for feedback repository here
        Task<List<Feedback>> GetAllFeedbacksAsync();

        Task<List<Feedback>> GetFeedbacksByCustomerIdAsync(Guid customerId);
        Task<Feedback?> GetFeedbackByOrderIdAsync(Guid orderId);

        Task<Feedback?> GetFeedbackByIdAsync(Guid feedbackId);
        Task<Feedback> CreateFeedbackAsync(Feedback feedback);

    }
}
