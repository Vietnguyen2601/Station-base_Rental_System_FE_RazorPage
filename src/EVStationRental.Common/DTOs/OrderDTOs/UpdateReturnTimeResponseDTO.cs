using System;

namespace EVStationRental.Common.DTOs.OrderDTOs
{
    /// <summary>
    /// Response DTO after updating return time
    /// </summary>
    public class UpdateReturnTimeResponseDTO
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime ReturnTime { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string VehicleStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
