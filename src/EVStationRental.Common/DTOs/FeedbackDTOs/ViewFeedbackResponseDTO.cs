using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Common.DTOs.FeedbackDTOs
{
    public class ViewFeedbackResponseDTO
    {
        public Guid FeedbackId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
