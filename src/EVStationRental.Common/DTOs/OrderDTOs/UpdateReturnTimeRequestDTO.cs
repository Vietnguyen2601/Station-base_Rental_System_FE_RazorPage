using System;

namespace EVStationRental.Common.DTOs.OrderDTOs
{
    /// <summary>
    /// DTO for updating order return time
    /// </summary>
    public class UpdateReturnTimeRequestDTO
    {
        public Guid OrderId { get; set; }
    }
}
