using System;
using System.Linq;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.WalletDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.Extensions.Logging;

namespace EVStationRental.Services.InternalServices.Services.WalletServices
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WalletService> _logger;

        public WalletService(IUnitOfWork unitOfWork, ILogger<WalletService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Get wallet balance for an account
        /// </summary>
        public async Task<IServiceResult> GetWalletBalanceAsync(Guid accountId)
        {
            try
            {
                var wallet = await _unitOfWork.WalletRepository.GetByAccountIdAsync(accountId);
                
                if (wallet == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Ví chưa được tạo cho tài khoản này"
                    };
                }

                var response = new WalletBalanceDTO
                {
                    WalletId = wallet.WalletId,
                    AccountId = wallet.AccountId,
                    Balance = wallet.Balance,
                    CreatedAt = wallet.CreatedAt,
                    UpdatedAt = wallet.UpdatedAt
                };

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Lấy thông tin ví thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet balance for account {AccountId}", accountId);
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy thông tin ví: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Top up wallet with transaction tracking
        /// </summary>
        public async Task<IServiceResult> TopUpWalletAsync(Guid accountId, TopUpWalletDTO request)
        {
            try
            {
                // Get or create wallet
                var wallet = await _unitOfWork.WalletRepository.GetByAccountIdAsync(accountId);
                
                if (wallet == null)
                {
                    // Auto-create wallet if not exists
                    var createResult = await CreateWalletForAccountAsync(accountId);
                    if (createResult.StatusCode != Const.SUCCESS_CREATE_CODE)
                    {
                        return createResult;
                    }
                    wallet = await _unitOfWork.WalletRepository.GetByAccountIdAsync(accountId);
                }

                // For simplicity, we'll process CASH/BANK_TRANSFER as immediate success
                // For VNPAY, you would generate a payment URL (similar to order payment)
                
                // Create transaction record first (PENDING for VNPay, COMPLETED for direct)
                var transaction = new WalletTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    OrderId = null, // Top-up is not related to any order
                    Amount = request.Amount,
                    TransactionType = TransactionType.DEPOSIT,
                    Description = request.Description ?? $"Nạp tiền vào ví qua {request.PaymentMethod}",
                    CreatedAt = DateTime.Now,
                    Isactive = true
                };

                await _unitOfWork.WalletRepository.CreateTransactionAsync(transaction);

                // Update wallet balance (for non-gateway methods)
                if (request.PaymentMethod.ToUpper() != "VNPAY")
                {
                    wallet.Balance += request.Amount;
                    wallet.UpdatedAt = DateTime.Now;
                    await _unitOfWork.WalletRepository.UpdateWalletAsync(wallet);

                    var response = new TopUpResponseDTO
                    {
                        TransactionId = transaction.TransactionId,
                        Amount = request.Amount,
                        PaymentMethod = request.PaymentMethod,
                        Status = "COMPLETED",
                        PaymentUrl = null,
                        Message = $"Nạp tiền thành công. Số dư hiện tại: {wallet.Balance:N0} VNĐ"
                    };

                    return new ServiceResult
                    {
                        StatusCode = Const.SUCCESS_CREATE_CODE,
                        Message = "Nạp tiền vào ví thành công",
                        Data = response
                    };
                }
                else
                {
                    // For VNPay: would need to integrate with VNPayService to generate payment URL
                    // For now, return a placeholder
                    var response = new TopUpResponseDTO
                    {
                        TransactionId = transaction.TransactionId,
                        Amount = request.Amount,
                        PaymentMethod = request.PaymentMethod,
                        Status = "PENDING",
                        PaymentUrl = "https://sandbox.vnpayment.vn/...", // Would be generated by VNPayService
                        Message = "Vui lòng hoàn tất thanh toán qua VNPay"
                    };

                    return new ServiceResult
                    {
                        StatusCode = Const.SUCCESS_CREATE_CODE,
                        Message = "Yêu cầu nạp tiền đã được tạo. Vui lòng thanh toán qua VNPay.",
                        Data = response
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error topping up wallet for account {AccountId}", accountId);
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi nạp tiền: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get transaction history for wallet
        /// </summary>
        public async Task<IServiceResult> GetTransactionHistoryAsync(Guid accountId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var wallet = await _unitOfWork.WalletRepository.GetByAccountIdAsync(accountId);
                
                if (wallet == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Ví chưa được tạo cho tài khoản này"
                    };
                }

                var transactions = await _unitOfWork.WalletRepository.GetTransactionHistoryAsync(
                    wallet.WalletId, pageNumber, pageSize);

                var response = transactions.Select(t => new WalletTransactionDTO
                {
                    TransactionId = t.TransactionId,
                    OrderId = t.OrderId,
                    OrderCode = t.Order?.OrderCode,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType.ToString(),
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                }).ToList();

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Lấy lịch sử giao dịch thành công",
                    Data = new
                    {
                        WalletId = wallet.WalletId,
                        CurrentBalance = wallet.Balance,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        Transactions = response
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction history for account {AccountId}", accountId);
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy lịch sử giao dịch: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Create wallet for a new account
        /// </summary>
        public async Task<IServiceResult> CreateWalletForAccountAsync(Guid accountId)
        {
            try
            {
                // Check if wallet already exists
                var existingWallet = await _unitOfWork.WalletRepository.GetByAccountIdAsync(accountId);
                if (existingWallet != null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_DATA_EXISTED_CODE,
                        Message = "Ví đã tồn tại cho tài khoản này"
                    };
                }

                var wallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    AccountId = accountId,
                    Balance = 0,
                    CreatedAt = DateTime.Now,
                    Isactive = true
                };

                await _unitOfWork.WalletRepository.CreateWalletAsync(wallet);

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_CREATE_CODE,
                    Message = "Tạo ví thành công",
                    Data = new WalletBalanceDTO
                    {
                        WalletId = wallet.WalletId,
                        AccountId = wallet.AccountId,
                        Balance = wallet.Balance,
                        CreatedAt = wallet.CreatedAt,
                        UpdatedAt = wallet.UpdatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wallet for account {AccountId}", accountId);
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi tạo ví: {ex.Message}"
                };
            }
        }
    }
}
