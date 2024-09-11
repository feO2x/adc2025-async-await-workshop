using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace AsyncVsSync.Backend;

public static class SyncVsAsyncEndpoints
{
    public static WebApplication MapSyncVsAsyncEndpoints(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.MapHealthChecks("/");
        app.MapGet("/sync", WaitSync);
        app.MapGet("/async", DelayAsync);
        app.MapGet("/results", GetThreadPoolResults);
        return app;
    }

    private static IResult WaitSync(int waitIntervalInMilliseconds, ThreadPoolWatcher threadPoolWatcher)
    {
        Thread.Sleep(waitIntervalInMilliseconds);
        threadPoolWatcher.UpdateUsedThreads();
        return Results.Ok();
    }

    private static async Task<IResult> DelayAsync(int waitIntervalInMilliseconds, ThreadPoolWatcher threadPoolWatcher)
    {
        await Task.Delay(waitIntervalInMilliseconds);
        threadPoolWatcher.UpdateUsedThreads();
        return Results.Ok();
    }

    private static IResult GetThreadPoolResults(ThreadPoolWatcher threadPoolWatcher)
    {
        var totalNumberOfThreads = ThreadPool.ThreadCount;
        var osDescription = $"{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}";
        var results = new
        {
            threadPoolWatcher.MaximumUsedWorkerThreads,
            threadPoolWatcher.MaximumWorkerThreads,
            totalNumberOfThreads,
            osDescription
        };
        threadPoolWatcher.Reset();
        return Results.Ok(results);
    }
}