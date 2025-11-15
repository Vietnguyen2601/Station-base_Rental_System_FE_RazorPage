using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Services.Base;
using EVStationRental.Services.ExternalService.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace EVStationRental.Services.ExternalService.Services
{
    public class PayOSService : IPayOSService
    {
        private readonly object? _payOSClient;
        private readonly Type? _payOSType;
        private readonly Type? _itemDataType;
        private readonly Type? _paymentDataType;
        private readonly ILogger<PayOSService> _logger;
        private readonly IConfiguration _configuration;

        public PayOSService(IConfiguration configuration, ILogger<PayOSService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var clientId = _configuration["PayOS:ClientId"];
            var apiKey = _configuration["PayOS:ApiKey"];
            var checksumKey = _configuration["PayOS:ChecksumKey"];

            try
            {
                // Load PayOS assembly and find types
                var assembly = Assembly.Load("PayOS");
                
                _payOSType = assembly.GetType("PayOS.PayOSClient");
                var payOSOptionsType = assembly.GetType("PayOS.PayOSOptions");
                _itemDataType = assembly.GetType("PayOS.Models.V2.PaymentRequests.PaymentLinkItem");
                _paymentDataType = assembly.GetType("PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest");

                if (_payOSType != null && payOSOptionsType != null)
                {
                    var options = Activator.CreateInstance(payOSOptionsType);
                    payOSOptionsType.GetProperty("ClientId")?.SetValue(options, clientId);
                    payOSOptionsType.GetProperty("ApiKey")?.SetValue(options, apiKey);
                    payOSOptionsType.GetProperty("ChecksumKey")?.SetValue(options, checksumKey);
                    
                    _payOSClient = Activator.CreateInstance(_payOSType, options);
                    _logger.LogInformation("PayOS client initialized successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize PayOS client");
                _payOSClient = null;
            }
        }

        /// <summary>
        /// Create payment link for wallet top-up
        /// </summary>
        public async Task<string> CreatePaymentLinkAsync(
            long orderCode,
            decimal amount,
            string description,
            string returnUrl,
            string cancelUrl)
        {
            try
            {
                if (_payOSClient == null)
                {
                    var mockUrl = $"{returnUrl}?orderCode={orderCode}&status=PAID";
                    _logger.LogWarning("PayOS client not initialized, using mock URL: {MockUrl}", mockUrl);
                    return mockUrl;
                }

                // PayOS requires integer amount in VND
                var amountInt = (int)amount;

                if (_itemDataType == null || _paymentDataType == null || _payOSType == null)
                {
                    throw new Exception("PayOS types not loaded properly");
                }

                // Create PaymentLinkItem instance
                var itemInstance = Activator.CreateInstance(_itemDataType);
                _itemDataType.GetProperty("Name")?.SetValue(itemInstance, "Nạp tiền vào ví");
                _itemDataType.GetProperty("Quantity")?.SetValue(itemInstance, 1);
                _itemDataType.GetProperty("Price")?.SetValue(itemInstance, amountInt);
                
                var itemsListType = typeof(List<>).MakeGenericType(_itemDataType);
                var itemsList = Activator.CreateInstance(itemsListType);
                itemsListType.GetMethod("Add")?.Invoke(itemsList, new[] { itemInstance });

                // Create CreatePaymentLinkRequest instance
                var paymentRequest = Activator.CreateInstance(_paymentDataType);
                _paymentDataType.GetProperty("OrderCode")?.SetValue(paymentRequest, orderCode);
                _paymentDataType.GetProperty("Amount")?.SetValue(paymentRequest, amountInt);
                _paymentDataType.GetProperty("Description")?.SetValue(paymentRequest, description);
                _paymentDataType.GetProperty("Items")?.SetValue(paymentRequest, itemsList);
                _paymentDataType.GetProperty("CancelUrl")?.SetValue(paymentRequest, cancelUrl);
                _paymentDataType.GetProperty("ReturnUrl")?.SetValue(paymentRequest, returnUrl);

                // Access PaymentRequests resource from PayOSClient
                var paymentRequestsProperty = _payOSType.GetProperty("PaymentRequests");
                var paymentRequestsResource = paymentRequestsProperty?.GetValue(_payOSClient);
                
                if (paymentRequestsResource == null)
                {
                    throw new Exception("PaymentRequests property not found on PayOSClient");
                }
                
                // Call CreateAsync method
                var createAsyncMethod = paymentRequestsResource.GetType().GetMethod("CreateAsync");
                if (createAsyncMethod == null)
                {
                    throw new Exception("CreateAsync method not found on PaymentRequests");
                }

                var task = (Task)createAsyncMethod.Invoke(paymentRequestsResource, new[] { paymentRequest, null })!;
                await task;
                
                var resultProperty = task.GetType().GetProperty("Result");
                var createPaymentResult = resultProperty?.GetValue(task);
                
                var checkoutUrlProperty = createPaymentResult?.GetType().GetProperty("CheckoutUrl");
                var checkoutUrl = (string)checkoutUrlProperty?.GetValue(createPaymentResult)!;

                return checkoutUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment link for OrderCode {OrderCode}", orderCode);
                throw;
            }
        }

        /// <summary>
        /// Get payment link information
        /// </summary>
        public async Task<IServiceResult> GetPaymentLinkInformationAsync(long orderCode)
        {
            try
            {
                if (_payOSClient != null && _payOSType != null)
                {
                    var method = _payOSType.GetMethod("getPaymentLinkInformation");
                    if (method != null)
                    {
                        var task = (Task<object>)method.Invoke(_payOSClient, new object[] { orderCode })!;
                        var paymentLinkInfo = await task;
                        return new ServiceResult
                        {
                            StatusCode = Const.SUCCESS_READ_CODE,
                            Message = "Lấy thông tin thanh toán thành công",
                            Data = paymentLinkInfo
                        };
                    }
                }
                
                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_READ_CODE,
                    Message = "Mock: Lấy thông tin thanh toán thành công",
                    Data = new { OrderCode = orderCode, Status = "PAID" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayOS payment information for OrderCode {OrderCode}", orderCode);
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi lấy thông tin thanh toán: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cancel payment link
        /// </summary>
        public async Task<IServiceResult> CancelPaymentLinkAsync(long orderCode, string? cancellationReason = null)
        {
            try
            {
                if (_payOSClient != null && _payOSType != null)
                {
                    var method = _payOSType.GetMethod("cancelPaymentLink");
                    if (method != null)
                    {
                        var task = (Task<object>)method.Invoke(_payOSClient, new object?[] { orderCode, cancellationReason })!;
                        var cancelResult = await task;
                        return new ServiceResult
                        {
                            StatusCode = Const.SUCCESS_UPDATE_CODE,
                            Message = "Hủy thanh toán thành công",
                            Data = cancelResult
                        };
                    }
                }
                
                return new ServiceResult
                {
                    StatusCode = Const.SUCCESS_UPDATE_CODE,
                    Message = "Mock: Hủy thanh toán thành công",
                    Data = new { OrderCode = orderCode, Status = "CANCELLED" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling PayOS payment for OrderCode {OrderCode}", orderCode);
                return new ServiceResult
                {
                    StatusCode = Const.ERROR_EXCEPTION,
                    Message = $"Lỗi khi hủy thanh toán: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Verify webhook signature
        /// </summary>
        public bool VerifyWebhookData(string webhookUrl, string receivedSignature)
        {
            try
            {
                // PayOS 2.0 webhook verification would be different
                // For now, return true as placeholder
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PayOS webhook data");
                return false;
            }
        }
    }
}
