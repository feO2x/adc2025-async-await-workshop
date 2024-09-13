using AsyncStreamingGrpc.AsyncStreaming;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Server;
using Serilog;

namespace AsyncStreamingGrpc.CompositionRoot;

public static class DependencyInjection
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder, ILogger logger)
    {
        builder.Host.UseSerilog(logger);
        builder
           .Services
           .AddAsyncStreamingExample()
           .AddHealthChecks()
           .Services
           .AddCodeFirstGrpc();
        return builder;
    }
}