using AsyncStreamingGrpc.AsyncStreaming;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace AsyncStreamingGrpc.CompositionRoot;

public static class HttpMiddleware
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.MapAsyncStreamingExample();
        app.MapHealthChecks("/");
        return app;
    }
}