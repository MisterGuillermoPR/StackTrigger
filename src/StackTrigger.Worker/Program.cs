using Microsoft.EntityFrameworkCore;
using StackTrigger.Application;
using StackTrigger.Infrastructure;
using StackTrigger.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

// Clean Architecture DI Registrations
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();

// Auto-apply EF Core database migrations on container startup
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<StackTriggerDbContext>();
        logger.LogInformation("Applying PostgreSQL database migrations if pending...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed during startup. Continuing service run.");
    }
}

await host.RunAsync();
