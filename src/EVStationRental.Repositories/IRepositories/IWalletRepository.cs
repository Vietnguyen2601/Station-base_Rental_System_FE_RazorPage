using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVStationRental.Repositories.Base;
using EVStationRental.Repositories.Models;

namespace EVStationRental.Repositories.IRepositories
{
    public interface IWalletRepository : IGenericRepository<Wallet>
    {
        Task<Wallet?> GetByAccountIdAsync(Guid accountId);
        Task<Wallet> CreateWalletAsync(Wallet wallet);
        Task<Wallet?> UpdateWalletAsync(Wallet wallet);
        Task<List<WalletTransaction>> GetTransactionHistoryAsync(Guid walletId, int pageNumber = 1, int pageSize = 20);
        Task<WalletTransaction> CreateTransactionAsync(WalletTransaction transaction);
    }
}
