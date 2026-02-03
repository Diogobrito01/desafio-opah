using System.Threading.RateLimiting;
using CashFlow.Transactions.API.Filters;
using CashFlow.Transactions.API.Middleware;
using CashFlow.Transactions.API.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using CashFlow.Transactions.Application;
using CashFlow.Transactions.Infrastructure;
using CashFlow.Transactions.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("Application", "CashFlow.Transactions.API")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
    
    options.Filters.Add<ModelStateValidationFilter>();
    options.Filters.Add<ValidationExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.WriteIndented = true;
})
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var jsonErrors = context.ModelState
            .Where(x => x.Key == "$" || x.Key.StartsWith("$."))
            .SelectMany(x => x.Value?.Errors ?? Enumerable.Empty<Microsoft.AspNetCore.Mvc.ModelBinding.ModelError>())
            .Select(e => e.ErrorMessage)
            .ToList();

        if (jsonErrors.Any())
        {
            var friendlyMessage = GetFriendlyJsonError(jsonErrors.First());
            
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Invalid JSON format",
                Status = StatusCodes.Status400BadRequest,
                Detail = friendlyMessage,
                Instance = context.HttpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            problemDetails.Extensions["hint"] = "A common fix is to replace comma (,) with period (.) in decimal numbers. Example: 250.00 instead of 250,00";

            return new BadRequestObjectResult(problemDetails);
        }

        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => FormatPropertyName(kvp.Key),
                kvp => kvp.Value!.Errors.Select(e => GetFriendlyValidationMessage(e.ErrorMessage)).ToArray()
            );

        var validationProblem = new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred. Please review the errors and correct your request.",
            Instance = context.HttpContext.Request.Path
        };

        validationProblem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(validationProblem);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "CashFlow Transactions API",
        Version = "v1",
        Description = "API for managing financial transactions (debits and credits)",
        Contact = new()
        {
            Name = "CashFlow Team",
            Email = "team@cashflow.com"
        }
    });
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "database",
        tags: new[] { "ready" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "ready" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["MessageBroker:Username"]}:{builder.Configuration["MessageBroker:Password"]}@{builder.Configuration["MessageBroker:Host"]}:5672",
        name: "rabbitmq",
        tags: new[] { "ready" });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

static string GetFriendlyJsonError(string technicalError)
{
    var lower = technicalError.ToLower();
    
    if (lower.Contains("invalid start of a property name") || 
        lower.Contains("expected a '\"'") ||
        lower.Contains("'0' is an invalid"))
    {
        return "Invalid JSON format detected.\n\n" +
               "How to fix:\n" +
               "Use PERIOD (.) instead of COMMA (,) for decimal numbers\n" +
               "Correct:   \"amount\": 250.00\n" +
               "Incorrect: \"amount\": 250,00\n\n" +
               "Ensure all property names are in double quotes\n" +
               "Check for missing or extra commas between properties\n\n" +
               "Tip: Validate your JSON at https://jsonlint.com before sending";
    }
    
    if (lower.Contains("unterminated string"))
    {
        return "Unterminated string in JSON.\n\n" +
               "Make sure all strings are properly enclosed in double quotes.";
    }
    
    if (lower.Contains("unexpected character") || lower.Contains("invalid character"))
    {
        return "Invalid character in JSON.\n\n" +
               "Check for:\n" +
               "Special characters that need escaping\n" +
               "Correct use of quotes (\" not ' for strings)\n" +
               "Comma (,) instead of period (.) in numbers";
    }
    
    return "Invalid JSON format.\n\n" +
           "Please validate your JSON syntax and ensure:\n" +
           "Use period (.) for decimal numbers, not comma (,)\n" +
           "All strings are in double quotes\n" +
           "Proper comma placement between properties";
}

static string FormatPropertyName(string propertyName)
{
    var cleanName = propertyName.Replace("command.", "").Replace("$.", "").Replace("$.command.", "").Trim();
    
    if (string.IsNullOrEmpty(cleanName) || cleanName == "$")
        return "request";
        
    return char.ToLowerInvariant(cleanName[0]) + cleanName.Substring(1);
}

static string GetFriendlyValidationMessage(string originalMessage)
{
    if (originalMessage.Contains("The command field is required"))
        return "Request body is required";
        
    return originalMessage;
}

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CashFlow Transactions API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseSerilogRequestLogging();

app.UseRateLimiter();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
});

// Apply database migrations (skip for Testing environment - used by integration tests)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        try
        {
            Log.Information("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying database migrations");
            throw;
        }
    }
}

Log.Information("Starting CashFlow Transactions API");

app.Run();

// Make Program class accessible to integration tests
namespace CashFlow.Transactions.API
{
    public partial class Program { }
}
