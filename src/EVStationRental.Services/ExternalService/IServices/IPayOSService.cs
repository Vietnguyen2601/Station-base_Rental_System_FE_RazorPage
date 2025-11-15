using EVStationRental.Services.Base;

namespace EVStationRental.Services.ExternalService.IServices
{
    public interface IPayOSService
    {
        /// <summary>
        /// Create payment link for wallet top-up
        /// </summary>
        Task<string> CreatePaymentLinkAsync(
            long orderCode,
            decimal amount,
            string description,
            string returnUrl,
            string cancelUrl);

        /// <summary>
        /// Get payment link information
        /// </summary>
        Task<IServiceResult> GetPaymentLinkInformationAsync(long orderCode);

        /// <summary>
        /// Cancel payment link
        /// </summary>
        Task<IServiceResult> CancelPaymentLinkAsync(long orderCode, string? cancellationReason = null);

        /// <summary>
        /// Verify webhook signature
        /// </summary>
        bool VerifyWebhookData(string webhookUrl, string receivedSignature);
    }
}
