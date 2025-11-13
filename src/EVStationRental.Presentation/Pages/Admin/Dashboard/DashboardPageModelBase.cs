using System;
using System.Text.Json;
using System.Threading.Tasks;
using EVStationRental.Services.Base;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Admin.Dashboard;

public abstract class DashboardPageModelBase : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    protected static async Task<T?> ReadResultAsync<T>(Func<Task<IServiceResult>> action)
    {
        try
        {
            var result = await action();
            return ConvertData<T>(result);
        }
        catch
        {
            return default;
        }
    }

    protected static T? ConvertData<T>(IServiceResult? result)
    {
        if (result?.Data == null)
        {
            return default;
        }

        if (result.Data is T typed)
        {
            return typed;
        }

        if (result.Data is JsonElement jsonElement)
        {
            return jsonElement.Deserialize<T>(JsonOptions);
        }

        var serialized = JsonSerializer.Serialize(result.Data, JsonOptions);
        return JsonSerializer.Deserialize<T>(serialized, JsonOptions);
    }
}
