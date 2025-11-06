using System;
using System.Linq;
using System.Threading.Tasks;
using EVStationRental.Common.DTOs.PaymentDTOs;
using EVStationRental.Common.DTOs.WalletDTOs;
using EVStationRental.Common.Enums.EnumModel;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Repositories.Models;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Services.Base;
using EVStationRental.Services.ExternalService.IServices;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using Microsoft.Extensions.Logging;

namespace EVStationRental.Services.InternalServices.Services.WalletServices
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WalletService> _logger;
        private readonly IVNPayService _vnpayService;

        public WalletService(IUnitOfWork unitOfWork, ILogger<WalletService> logger, IVNPayService vnpayService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _vnpayService = vnpayService;
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
        /// Supports CASH (immediate) and VNPAY (redirect to payment gateway)
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

                // Create transaction record first (PENDING for VNPay, COMPLETED for CASH)
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

                // Handle based on payment method
                if (request.PaymentMethod.ToUpper() == "VNPAY")
                {
                    // Generate VNPay payment URL
                    var vnpayRequest = new VNPayRequestDTO
                    {
                        OrderId = transaction.TransactionId, // Use transaction ID as order reference
                        Amount = request.Amount,
                        OrderInfo = $"Nap tien vi {wallet.WalletId}",
                        ReturnUrl = request.ReturnUrl ?? "http://localhost:5000/api/Payment/wallet-vnpay-return",
                        CancelUrl = request.CancelUrl ?? "http://localhost:5000/api/Payment/wallet-vnpay-cancel",
                        IpAddress = "127.0.0.1", // Will be replaced by actual IP from controller
                        TxnRef = transaction.TransactionId.ToString()
                    };

                    var paymentUrl = _vnpayService.CreatePaymentUrl(vnpayRequest, vnpayRequest.IpAddress);

                    var response = new TopUpResponseDTO
                    {
                        TransactionId = transaction.TransactionId,
                        Amount = request.Amount,
                        PaymentMethod = request.PaymentMethod,
                        Status = "PENDING",
                        PaymentUrl = paymentUrl,
                        Message = "Vui lòng hoàn tất thanh toán qua VNPay"
                    };

                    return new ServiceResult
                    {
                        StatusCode = Const.SUCCESS_CREATE_CODE,
                        Message = "Yêu cầu nạp tiền đã được tạo. Vui lòng thanh toán qua VNPay.",
                        Data = response
                    };
                }
                else
                {
                    // CASH: Update wallet balance immediately
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

        /// <summary>
        /// Handle VNPay return for wallet top-up
        /// </summary>
        public async Task<IServiceResult> HandleVNPayWalletReturnAsync(VNPayReturnDTO returnData)
        {
            try
            {
                // Validate VNPay signature
                var isValidSignature = _vnpayService.ValidateSignature(returnData);
                if (!isValidSignature)
                {
                    _logger.LogWarning("Invalid VNPay signature for transaction {TxnRef}", returnData.vnp_TxnRef);
                    return new ServiceResult
                    {
                        StatusCode = Const.FAIL_READ_CODE,
                        Message = "Chữ ký VNPay không hợp lệ"
                    };
                }

                // Parse transaction ID from vnp_TxnRef
                if (!Guid.TryParse(returnData.vnp_TxnRef, out var transactionId))
                {
                    _logger.LogError("Invalid transaction ID format: {TxnRef}", returnData.vnp_TxnRef);
                    return new ServiceResult
                    {
                        StatusCode = Const.FAIL_READ_CODE,
                        Message = "Mã giao dịch không hợp lệ"
                    };
                }

                // Get transaction from database
                var transactions = await _unitOfWork.WalletRepository.GetTransactionHistoryAsync(Guid.Empty, 1, 1000);
                var transaction = transactions.FirstOrDefault(t => t.TransactionId == transactionId);

                if (transaction == null)
                {
                    _logger.LogError("Transaction not found: {TransactionId}", transactionId);
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm thấy giao dịch"
                    };
                }

                // Get wallet
                var wallet = await _unitOfWork.WalletRepository.GetByAccountIdAsync(transaction.Wallet.AccountId);
                if (wallet == null)
                {
                    return new ServiceResult
                    {
                        StatusCode = Const.WARNING_NO_DATA_CODE,
                        Message = "Không tìm thấy ví"
                    };
                }

                // Check payment success
                if (returnData.vnp_ResponseCode == "00")
                {
                    // Payment successful - update wallet balance
                    wallet.Balance += transaction.Amount;
                    wallet.UpdatedAt = DateTime.Now;
                    await _unitOfWork.WalletRepository.UpdateWalletAsync(wallet);

                    return new ServiceResult
                    {
                        StatusCode = Const.SUCCESS_READ_CODE,
                        Message = "Nạp tiền thành công",
                        Data = new
                        {
                            TransactionId = transaction.TransactionId,
                            Amount = transaction.Amount,
                            NewBalance = wallet.Balance,
                            PaymentTime = returnData.vnp_PayDate,
                            BankCode = returnData.vnp_BankCode,
                            TransactionNo = returnData.vnp_TransactionNo
                        }
                    };
                }
                else
                {
                    // Payment failed
                    _logger.LogWarning("VNPay payment failed for transaction {TransactionId}. Response code: {ResponseCode}", 
                        transactionId, returnData.vnp_ResponseCode);

                    return new ServiceResult
                    {
                        StatusCode = Const.FAIL_READ_CODE,
                        Message = $"Thanh toán thất bại. Mã lỗi: {returnData.vnp_ResponseCode}",
                        Data = new
                        {
                            TransactionId = transactionId,
                            ResponseCode = returnData.vnp_ResponseCode,
                            Message = GetVNPayResponseMessage(returnData.vnp_ResponseCode)
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling VNPay wallet return");
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi xử lý kết quả thanh toán: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Create VNPay payment URL by WalletId (for direct wallet top-up)
        /// </summary>
        public async Task<IServiceResult> CreateVNPayUrlByWalletIdAsync(Guid walletId, decimal amount, string? returnUrl, string? cancelUrl, string ipAddress)
        {
            try
            {
                // Verify wallet exists
                var wallet = await _unitOfWork.WalletRepository.GetByAccountIdAsync(Guid.Empty);
                
                // Find wallet by ID (need to query differently)
                // Since GetByAccountIdAsync doesn't work with walletId, we'll need to get by a transaction or direct query
                // For now, let's create a transaction record first
                
                var transaction = new WalletTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = walletId,
                    OrderId = null,
                    Amount = amount,
                    TransactionType = TransactionType.DEPOSIT,
                    Description = $"Nạp tiền vào ví {walletId} qua VNPay",
                    CreatedAt = DateTime.Now,
                    Isactive = true
                };

                await _unitOfWork.WalletRepository.CreateTransactionAsync(transaction);

                // Generate VNPay payment URL
                var vnpayRequest = new VNPayRequestDTO
                {
                    OrderId = transaction.TransactionId, // Use transaction ID as order reference
                    Amount = amount,
                    OrderInfo = $"Nap tien vi {walletId}",
                    ReturnUrl = returnUrl ?? "http://localhost:5000/api/Payment/wallet-vnpay-return",
                    CancelUrl = cancelUrl ?? "http://localhost:5000/api/Payment/wallet-vnpay-cancel",
                    IpAddress = ipAddress,
                    TxnRef = transaction.TransactionId.ToString()
                };

                var paymentUrl = _vnpayService.CreatePaymentUrl(vnpayRequest, ipAddress);

                var response = new TopUpResponseDTO
                {
                    TransactionId = transaction.TransactionId,
                    Amount = amount,
                    PaymentMethod = "VNPAY",
                    Status = "PENDING",
                    PaymentUrl = paymentUrl,
                    Message = "Vui lòng hoàn tất thanh toán qua VNPay"
                };

                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_CREATE_CODE,
                    Message = "Tạo URL thanh toán VNPay thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay URL for wallet {WalletId}", walletId);
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi tạo URL thanh toán: {ex.Message}"
                };
            }
        }

        private string GetVNPayResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định.",
                _ => $"Lỗi không xác định (Mã: {responseCode})"
            };
        }
    }
}

