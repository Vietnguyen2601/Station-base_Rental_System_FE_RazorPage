using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Common.DTOs.FeedbackDTOs
{
    public class CreateFeedbackRequestDTO
    {
        public Guid CustomerId { get; set; }

        public Guid OrderId { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }

    }
}
