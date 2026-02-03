using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CashFlow.Consolidation.API.ModelBinders;

/// <summary>
/// Custom model binder to support multiple date formats including Brazilian format (DD-MM-YYYY)
/// </summary>
public class DateTimeModelBinder : IModelBinder
{
    private static readonly string[] DateFormats = new[]
    {
        "dd-MM-yyyy",           // Brazilian: 03-02-2026
        "dd/MM/yyyy",           // Brazilian with slash: 03/02/2026
        "yyyy-MM-dd",           // ISO: 2026-02-03
        "yyyy/MM/dd",           // ISO with slash: 2026/02/03
        "dd-MM-yyyy HH:mm:ss",  // Brazilian with time
        "dd/MM/yyyy HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",  // ISO with time
        "yyyy-MM-ddTHH:mm:ssZ", // ISO with timezone
        "yyyy-MM-ddTHH:mm:ss.fffZ" // ISO with milliseconds
    };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.CompletedTask;
        }

        // Try to parse with custom formats first
        if (DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, 
            DateTimeStyles.None, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
            return Task.CompletedTask;
        }

        // Try standard DateTime parsing as fallback
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
            return Task.CompletedTask;
        }

        // If all parsing attempts fail
        bindingContext.ModelState.TryAddModelError(
            modelName,
            $"Invalid date format. Accepted formats: DD-MM-YYYY, DD/MM/YYYY, YYYY-MM-DD, ISO 8601");

        return Task.CompletedTask;
    }
}
