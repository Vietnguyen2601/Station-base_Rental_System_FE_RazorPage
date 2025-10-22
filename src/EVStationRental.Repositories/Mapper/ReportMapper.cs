using EVStationRental.Common.DTOs.ReportDTOs;
using EVStationRental.Repositories.Models;

namespace EVStationRental.Repositories.Mapper
{
    public static class ReportMapper
    {
        public static ViewReportResponse ToViewReportResponse(this Report report)
        {
            return new ViewReportResponse
            {
                ReportId = report.ReportId,
                ReportType = report.ReportType,
                GeneratedDate = report.GeneratedDate,
                Text = report.Text,
                AccountUsername = report.Account?.Username ?? string.Empty,
                VehicleSerialNumber = report.Vehicle?.SerialNumber ?? string.Empty
            };
        }
    }
}
