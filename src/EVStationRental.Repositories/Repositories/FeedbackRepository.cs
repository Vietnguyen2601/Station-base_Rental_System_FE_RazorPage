using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly ElectricVehicleDBContext _context;
        public FeedbackRepository(ElectricVehicleDBContext context)
        {
            _context = context;
        }

        public async Task<Feedback> CreateFeedbackAsync(Feedback feedback)
        {
            _context.Set<Feedback>().Add(feedback);
            await _context.SaveChangesAsync();
            return feedback;
        }

        public async Task<List<Feedback>> GetAllFeedbacksAsync()
        {
            return await _context.Set<Feedback>().ToListAsync();
        }

        public async Task<Feedback?> GetFeedbackByIdAsync(Guid feedbackId)
        {
            return await _context.Set<Feedback>().FindAsync(feedbackId);
        }

        public async Task<Feedback?> GetFeedbackByOrderIdAsync(Guid orderId)
        {
            return await _context.Set<Feedback>().FirstOrDefaultAsync(f => f.OrderId == orderId);
        }

        public async Task<List<Feedback>> GetFeedbacksByCustomerIdAsync(Guid customerId)
        {
            return await _context.Set<Feedback>().Where(f => f.CustomerId == customerId).ToListAsync();
        }
    }
}
