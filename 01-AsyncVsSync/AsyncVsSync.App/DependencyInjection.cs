using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncVsSync.App;

public static class DependencyInjection
{
    public static ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        return new ServiceCollection()
           .AddSingleton<IConfiguration>(configuration)
           .AddSerilogLogging()
           .AddHttpClient()
           .BuildServiceProvider();
    }
}