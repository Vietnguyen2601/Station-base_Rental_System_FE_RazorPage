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

        public static CreateReportResponseDTO ToCreateReportResponse(this Report report)
        {
            return new CreateReportResponseDTO
            {
                ReportId = report.ReportId,
                ReportType = report.ReportType,
                GeneratedDate = report.GeneratedDate,
                Text = report.Text,
                AccountId = report.AccountId,
                AccountUsername = report.Account?.Username ?? string.Empty,
                AccountRole = report.Account?.Role?.RoleName ?? string.Empty,
                VehicleId = report.VehicleId,
                VehicleSerialNumber = report.Vehicle?.SerialNumber ?? string.Empty,
                CreatedAt = report.CreatedAt,
                Isactive = report.Isactive,
                UpdateAt = report.UpdatedAt
            };
        }

        public static Report ToReport(this CreateReportRequestDTO dto)
        {
            return new Report
            {
                ReportId = Guid.NewGuid(),
                ReportType = dto.ReportType,
                GeneratedDate = DateTime.UtcNow,
                Text = dto.Text,
                AccountId = dto.AccountId,
                VehicleId = dto.VehicleId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                Isactive = true
            };
        }

        public static void UpdateReportFromDto(this Report report, UpdateReportRequestDTO dto)
        {
            report.ReportType = dto.ReportType;
            report.Text = dto.Text;
            report.UpdatedAt = DateTime.Now;
        }
    }
}
