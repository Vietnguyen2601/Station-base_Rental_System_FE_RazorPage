using System;
using System.Collections.Generic;

namespace EVStationRental.Presentation.Models.Admin.Dashboard;

public class StationOrderCountResponse
{
    public string? FilterType { get; set; }
    public string? FilterDescription { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<StationOrderCountItem> Stations { get; set; } = new();
}

public class StationOrderCountItem
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int OngoingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CanceledOrders { get; set; }
}
