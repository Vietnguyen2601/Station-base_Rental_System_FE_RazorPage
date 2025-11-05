using EVStationRental.Common.DTOs.PaymentDTOs;
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
        Task<IServiceResult> HandleVNPayWalletReturnAsync(VNPayReturnDTO returnData);
        Task<IServiceResult> CreateVNPayUrlByWalletIdAsync(Guid walletId, decimal amount, string? returnUrl, string? cancelUrl, string ipAddress);
    }
}
