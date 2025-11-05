using EVStationRental.Common.DTOs.WalletDTOs;
using EVStationRental.Services.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EVStationRental.Services.InternalServices.IServices.IWalletServices
{
    public interface IWalletService
    {
        Task<IServiceResult> GetWalletBalanceAsync(Guid accountId);
        Task<IServiceResult> TopUpWalletAsync(Guid accountId, TopUpWalletDTO request);
        Task<IServiceResult> GetTransactionHistoryAsync(Guid accountId, int pageNumber = 1, int pageSize = 20);
        Task<IServiceResult> CreateWalletForAccountAsync(Guid accountId);
    }
}
