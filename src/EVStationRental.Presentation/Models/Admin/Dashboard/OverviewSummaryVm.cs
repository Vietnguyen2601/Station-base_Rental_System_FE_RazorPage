using System.Linq;
using EVStationRental.Common.DTOs.DashboardDTOs;

namespace EVStationRental.Presentation.Models.Admin.Dashboard;

public class OverviewSummaryVm
{
    public string FilterDescription { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Ongoing { get; set; }
    public int Completed { get; set; }
    public int Canceled { get; set; }

    public static OverviewSummaryVm From(
        StationRevenueDashboardResponseDTO revenue,
        StationOrderCountResponse orders)
    {
        return new OverviewSummaryVm
        {
            FilterDescription = revenue.FilterDescription ?? orders.FilterDescription ?? string.Empty,
            TotalRevenue = revenue.TotalRevenue,
            TotalOrders = revenue.TotalOrders,
            Pending = orders.Stations.Sum(s => s.PendingOrders),
            Confirmed = orders.Stations.Sum(s => s.ConfirmedOrders),
            Ongoing = orders.Stations.Sum(s => s.OngoingOrders),
            Completed = orders.Stations.Sum(s => s.CompletedOrders),
            Canceled = orders.Stations.Sum(s => s.CanceledOrders)
        };
    }
}
