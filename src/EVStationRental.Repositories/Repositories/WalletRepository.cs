using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVStationRental.Repositories.Base;
using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace EVStationRental.Repositories.Repositories
{
    public class WalletRepository : GenericRepository<Wallet>, IWalletRepository
    {
        private readonly ElectricVehicleDBContext _context;

        public WalletRepository(ElectricVehicleDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Wallet?> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.Wallets
                .Include(w => w.Account)
                .Include(w => w.WalletTransactions.OrderByDescending(t => t.CreatedAt).Take(10))
                .FirstOrDefaultAsync(w => w.AccountId == accountId && w.Isactive);
        }

        public async Task<Wallet> CreateWalletAsync(Wallet wallet)
        {
            wallet.CreatedAt = DateTime.Now;
            wallet.Isactive = true;
            wallet.Balance = 0; // Start with 0 balance
            
            await _context.Wallets.AddAsync(wallet);
            await _context.SaveChangesAsync();
            return wallet;
        }

        public async Task<Wallet?> UpdateWalletAsync(Wallet wallet)
        {
            var existingWallet = await _context.Wallets
                .AsTracking()
                .FirstOrDefaultAsync(w => w.WalletId == wallet.WalletId);

            if (existingWallet == null)
                return null;

            existingWallet.Balance = wallet.Balance;
            existingWallet.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return existingWallet;
        }

        public async Task<List<WalletTransaction>> GetTransactionHistoryAsync(Guid walletId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.WalletTransactions
                .Include(t => t.Order)
                .Where(t => t.WalletId == walletId && t.Isactive)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<WalletTransaction> CreateTransactionAsync(WalletTransaction transaction)
        {
            transaction.CreatedAt = DateTime.Now;
            transaction.Isactive = true;

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }
    }
}
