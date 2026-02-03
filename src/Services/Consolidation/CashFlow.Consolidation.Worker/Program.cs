using CashFlow.BuildingBlocks.EventBus.RabbitMQ;
using CashFlow.Consolidation.Application;
using CashFlow.Consolidation.Infrastructure;
using CashFlow.Consolidation.Infrastructure.Persistence;
using CashFlow.Consolidation.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting CashFlow Consolidation Worker");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog
    builder.Services.AddSerilog(lc => lc
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.WithProperty("Application", "CashFlow.Consolidation.Worker")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console());

    // Add Application and Infrastructure layers
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Add RabbitMQ settings
    builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("MessageBroker"));

    // Add the worker
    builder.Services.AddHostedService<TransactionEventConsumer>();

    var host = builder.Build();

    // Run database migrations
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ConsolidationDbContext>();
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

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
