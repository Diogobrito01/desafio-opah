using CashFlow.Transactions.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CashFlow.IntegrationTests;

/// <summary>
/// Factory for creating test instances of the Transactions API
/// Configures in-memory database for testing
/// </summary>
public class TransactionsApiFactory : WebApplicationFactory<CashFlow.Transactions.API.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Testing environment
        builder.UseEnvironment("Testing");
        
        builder.ConfigureTestServices(services =>
        {
            // Remove the actual database context
            services.RemoveAll(typeof(DbContextOptions<TransactionsDbContext>));

            // Add in-memory database for testing
            services.AddDbContext<TransactionsDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<TransactionsDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();
        });
    }
}
