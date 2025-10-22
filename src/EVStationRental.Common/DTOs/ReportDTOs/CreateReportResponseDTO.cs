using System;

namespace EVStationRental.Common.DTOs.ReportDTOs
{
    public class CreateReportResponseDTO
    {
        public Guid ReportId { get; set; }
        public string ReportType { get; set; } = null!;
        public DateTime GeneratedDate { get; set; }
        public string Text { get; set; } = null!;
        public Guid AccountId { get; set; }
        public string AccountUsername { get; set; } = null!;
        public string AccountRole { get; set; } = null!;
        public Guid VehicleId { get; set; }
        public string VehicleSerialNumber { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool Isactive { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
