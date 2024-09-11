using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AsyncVsSync.Backend;

public static class DependencyInjection
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(
            (context, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(context.Configuration)
        );
        builder.Services
           .AddSingleton(new ThreadPoolWatcher())
           .AddHealthChecks();
        return builder;
    }
}