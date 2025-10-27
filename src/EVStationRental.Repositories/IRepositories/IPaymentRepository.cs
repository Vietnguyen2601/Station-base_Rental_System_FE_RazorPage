using EVStationRental.Repositories.Base;
using EVStationRental.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByOrderIdAsync(Guid orderId);
        Task<Payment?> GetByGatewayTxIdAsync(string gatewayTxId);
        Task<List<Payment>> GetPaymentsByOrderIdAsync(Guid orderId);
        Task<Payment?> GetLatestPaymentByOrderIdAsync(Guid orderId);
    }
}