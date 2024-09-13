using System.Threading.Tasks;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.DatabaseAccess;

public static class DatabaseAccessModule
{
    public static IServiceCollection AddDatabaseAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration
           .GetConnectionString(nameof(WebApiDbContext))
           .MustNotBeNullOrWhiteSpace(
                _ => new InvalidConfigurationException("The WebApiDbContext connection string is missing")
            );

        services
           .AddDbContext<WebApiDbContext>(options => options.UseNpgsql(connectionString))
           .AddScoped<EfSeedDatabaseSession>();


        return services;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("databaseAccess:migrateDatabaseOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WebApiDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("databaseAccess:seedDatabaseOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbSession = scope.ServiceProvider.GetRequiredService<EfSeedDatabaseSession>();
        await dbSession.InsertSeedDataAsync();
        await dbSession.SaveChangesAsync();
    }
}