using System;

namespace EVStationRental.Common.DTOs.ReportDTOs
{
    public class ViewReportResponse
    {
        public Guid ReportId { get; set; }
        public string ReportType { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string Text { get; set; }
        public string AccountUsername { get; set; }
        public string VehicleSerialNumber { get; set; }
    }
}
