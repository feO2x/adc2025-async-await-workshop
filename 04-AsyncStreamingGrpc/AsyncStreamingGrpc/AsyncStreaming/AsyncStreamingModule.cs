using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AsyncStreamingGrpc.AsyncStreaming;

public static class AsyncStreamingModule
{
    public static IServiceCollection AddAsyncStreamingExample(this IServiceCollection services) =>
        services.AddSingleton<NumberGenerator>()
           .AddOptions<NumberGeneratorOptions>()
           .BindConfiguration("asyncStreaming")
           .Services
           .AddSingleton(sp => sp.GetRequiredService<IOptions<NumberGeneratorOptions>>().Value);

    public static void MapAsyncStreamingExample(this WebApplication app) =>
        app.MapGrpcService<NumberStreamingService>();
}