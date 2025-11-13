using System;
using System.Collections.Generic;
using System.Linq;
using EVStationRental.Common.DTOs.DashboardDTOs;
using EVStationRental.Presentation.Models.Admin.Dashboard;

namespace EVStationRental.Presentation.Helpers;

public static class DashboardFilterHelper
{
    public const string PresetMonth = "month";
    public const string PresetQuarter = "quarter";
    public const string PresetYear = "year";
    public const string PresetCustom = "custom";

    public static string Normalize(DashboardFilterDTO filter, string? preset)
    {
        var normalized = string.IsNullOrWhiteSpace(preset)
            ? PresetMonth
            : preset.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        switch (normalized)
        {
            case PresetMonth:
                filter.Month ??= now.Month;
                filter.Year ??= now.Year;
                filter.Quarter = null;
                filter.StartDate = null;
                filter.EndDate = null;
                break;
            case PresetQuarter:
                filter.Quarter ??= (now.Month - 1) / 3 + 1;
                filter.Year ??= now.Year;
                filter.Month = null;
                filter.StartDate = null;
                filter.EndDate = null;
                break;
            case PresetYear:
                filter.Year ??= now.Year;
                filter.Month = null;
                filter.Quarter = null;
                filter.StartDate = null;
                filter.EndDate = null;
                break;
            case PresetCustom:
                if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
                {
                    filter.StartDate = now.Date.AddDays(-30);
                    filter.EndDate = now.Date;
                }
                filter.Month = null;
                filter.Quarter = null;
                filter.Year = null;
                break;
            default:
                normalized = PresetMonth;
                filter.Month ??= now.Month;
                filter.Year ??= now.Year;
                filter.Quarter = null;
                filter.StartDate = null;
                filter.EndDate = null;
                break;
        }

        return normalized;
    }

    public static IReadOnlyList<int> GetYearOptions(int count = 5)
    {
        var currentYear = DateTime.UtcNow.Year;
        return Enumerable.Range(0, count)
            .Select(offset => currentYear - offset)
            .ToList();
    }

    public static DashboardFilterViewModel BuildViewModel(
        DashboardFilterDTO filter,
        string preset,
        string formAction,
        bool showSearchBox = false,
        string searchInputName = "q",
        string? searchQuery = null)
    {
        return new DashboardFilterViewModel
        {
            Filter = filter,
            SelectedPreset = preset,
            FormAction = formAction,
            YearOptions = GetYearOptions(),
            ShowSearchBox = showSearchBox,
            SearchInputName = searchInputName,
            SearchQuery = searchQuery
        };
    }
}
