using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CashFlow.Transactions.API.Filters;

/// <summary>
/// Filter to customize ModelState validation error messages
/// </summary>
public class ModelStateValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => FormatPropertyName(kvp.Key),
                    kvp => kvp.Value!.Errors.Select(e => GetFriendlyErrorMessage(e.ErrorMessage, kvp.Key)).ToArray()
                );

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more fields have invalid values. Please check the errors and try again.",
                Instance = context.HttpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            context.Result = new BadRequestObjectResult(problemDetails);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }

    private static string FormatPropertyName(string propertyName)
    {
        // Remove prefixes like "command." or "$."
        var cleanName = propertyName.Replace("command.", "").Replace("$.", "").Replace("$.command.", "");
        
        // Convert to camelCase
        if (string.IsNullOrEmpty(cleanName))
            return "request";

        return char.ToLowerInvariant(cleanName[0]) + cleanName[1..];
    }

    private static string GetFriendlyErrorMessage(string originalMessage, string propertyName)
    {
        var lowerMessage = originalMessage.ToLower();

        // Detect JSON parsing errors
        if (lowerMessage.Contains("invalid start") || lowerMessage.Contains("expected a '\"'"))
        {
            return "Invalid JSON format detected. Please use decimal point (.) instead of comma (,) for decimal numbers. Example: use 250.00 instead of 250,00";
        }

        if (lowerMessage.Contains("could not convert") || lowerMessage.Contains("invalid cast"))
        {
            return $"Invalid value format for '{FormatPropertyName(propertyName)}'. Please check the expected data type.";
        }

        if (lowerMessage.Contains("required"))
        {
            return $"The field '{FormatPropertyName(propertyName)}' is required.";
        }

        // Return original message if no specific case matches
        return originalMessage;
    }
}
