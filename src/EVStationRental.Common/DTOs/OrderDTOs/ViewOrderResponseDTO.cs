using EVStationRental.Common.Enums.EnumModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Common.DTOs.OrderDTOs
{
    public class ViewOrderResponseDTO
    {
        public Guid OrderId { get; set; }

        public Guid CustomerId { get; set; }

        public Guid VehicleId { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public decimal BasePrice { get; set; }

        public decimal TotalPrice { get; set; }

        public Guid? PromotionId { get; set; }

        public Guid? StaffId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool Isactive { get; set; }

        public OrderStatus Status { get; set; }
    }
}
