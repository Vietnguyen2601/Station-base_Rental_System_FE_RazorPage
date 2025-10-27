using EVStationRental.Common.DTOs.PaymentDTOs;
using EVStationRental.Common.Enums.ServiceResultEnum;
using EVStationRental.Services.Base;
using EVStationRental.Services.ExternalService.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EVStationRental.Services.ExternalService.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VNPayService> _logger;

        public VNPayService(IConfiguration configuration, ILogger<VNPayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string CreatePaymentUrl(VNPayRequestDTO request, string ipAddress)
        {
            try
            {
                var vnpay = new VNPayLibrary();
                
                var vnp_TxnRef = $"{request.OrderId}_{DateTime.Now.Ticks}";
                var vnp_Amount = ((long)(request.Amount * 100)).ToString(); // VNPay uses VND * 100
                
                // Handle OrderInfo - use default if request OrderInfo is null, empty, or "string"
                var vnp_OrderInfo = !string.IsNullOrEmpty(request.OrderInfo) && request.OrderInfo != "string" 
                    ? request.OrderInfo 
                    : $"Thanh toan don hang {request.OrderId}";

                vnpay.AddRequestData("vnp_Version", _configuration["VNPay:Version"]);
                vnpay.AddRequestData("vnp_Command", _configuration["VNPay:Command"]);
                vnpay.AddRequestData("vnp_TmnCode", _configuration["VNPay:TmnCode"]);
                vnpay.AddRequestData("vnp_Amount", vnp_Amount);
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);
                vnpay.AddRequestData("vnp_OrderInfo", vnp_OrderInfo);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_IpAddr", ipAddress);
                
                // Handle ReturnUrl - use config if request ReturnUrl is null, empty, or "string"
                var returnUrl = !string.IsNullOrEmpty(request.ReturnUrl) && request.ReturnUrl != "string" 
                    ? request.ReturnUrl 
                    : _configuration["VNPay:ReturnUrl"];
                vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
                
                // Add cancel URL if provided and not "string"
                if (!string.IsNullOrEmpty(request.CancelUrl) && request.CancelUrl != "string")
                {
                    vnpay.AddRequestData("vnp_CancelUrl", request.CancelUrl);
                }
                
                // Remove vnp_BankCode completely - VNPay will handle bank selection

                // Debug logging
                _logger.LogInformation("VNPay Request Data: TmnCode={TmnCode}, Amount={Amount}, TxnRef={TxnRef}, ReturnUrl={ReturnUrl}", 
                    _configuration["VNPay:TmnCode"], vnp_Amount, vnp_TxnRef, returnUrl);
                
                _logger.LogInformation("Full VNPay Request Parameters:");
                foreach (var kv in vnpay.RequestData)
                {
                    _logger.LogInformation("  {Key}: {Value}", kv.Key, kv.Value);
                }

                var paymentUrl = vnpay.CreateRequestUrl(_configuration["VNPay:PaymentUrl"], _configuration["VNPay:HashSecret"]);
                
                _logger.LogInformation("Generated VNPay URL: {PaymentUrl}", paymentUrl);
                _logger.LogInformation("PaymentUrl length: {Length}", paymentUrl.Length);
                
                _logger.LogInformation("VNPay payment URL created successfully for Order {OrderId}", request.OrderId);
                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment URL for Order {OrderId}", request.OrderId);
                return string.Empty;
            }
        }

        public IServiceResult ProcessReturnUrl(VNPayReturnDTO returnData)
        {
            try
            {
                if (!ValidateSignature(returnData))
                {
                    _logger.LogWarning("Invalid VNPay signature");
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Invalid signature");
                }

                var vnpResponseCode = returnData.vnp_ResponseCode;
                var vnpTransactionStatus = returnData.vnp_TransactionStatus;

                if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
                {
                    // Payment successful
                    var result = new
                    {
                        OrderRef = returnData.vnp_TxnRef,
                        Amount = decimal.Parse(returnData.vnp_Amount) / 100, // Convert back from VND * 100
                        BankCode = returnData.vnp_BankCode,
                        TransactionNo = returnData.vnp_TransactionNo,
                        PayDate = DateTime.ParseExact(returnData.vnp_PayDate, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                        Status = "SUCCESS"
                    };

                    _logger.LogInformation("VNPay payment successful. TxnRef: {TxnRef}", returnData.vnp_TxnRef);
                    return new ServiceResult(Const.SUCCESS_PAYMENT_CODE, "Payment successful", result);
                }
                else
                {
                    _logger.LogWarning("VNPay payment failed. ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}", 
                        vnpResponseCode, vnpTransactionStatus);
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Payment failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay return URL");
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Error processing return URL: {ex.Message}");
            }
        }

        public bool ValidateSignature(VNPayReturnDTO returnData)
        {
            try
            {
                var vnpay = new VNPayLibrary();
                foreach (var prop in returnData.GetType().GetProperties())
                {
                    var value = prop.GetValue(returnData)?.ToString();
                    if (!string.IsNullOrEmpty(value) && prop.Name != "vnp_SecureHash")
                    {
                        vnpay.AddResponseData(prop.Name, value);
                    }
                }

                var isValidSignature = vnpay.ValidateSignature(returnData.vnp_SecureHash, _configuration["VNPay:HashSecret"]);
                return isValidSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating VNPay signature");
                return false;
            }
        }
    }

    // VNPay Library Helper Class
    public class VNPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public SortedList<string, string> RequestData => _requestData;

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var data = new StringBuilder();
            
            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var queryString = data.ToString();
            baseUrl += "?" + queryString;
            
            var signDataString = queryString;
            if (signDataString.Length > 0)
            {
                signDataString = signDataString.Remove(signDataString.Length - 1, 1);
            }

            var vnpSecureHash = Utils.HmacSHA512(vnpHashSecret, signDataString);
            baseUrl += "vnp_SecureHash=" + vnpSecureHash;

            // Debug logging for signature
            Console.WriteLine($"Sign Data: {signDataString}");
            Console.WriteLine($"Hash Secret: {vnpHashSecret}");  
            Console.WriteLine($"Generated Hash: {vnpSecureHash}");

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rspRaw = GetResponseData();
            var myChecksum = Utils.HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }

            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            foreach (var kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }

    public class Utils
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }
    }
}