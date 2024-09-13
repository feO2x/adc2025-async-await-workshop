using System;
using System.Threading.Tasks;
using AsyncStreamingGrpc.CompositionRoot;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace AsyncStreamingGrpc;

public sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = Logging.CreateLogger();
        try
        {
            await using var app = WebApplication
               .CreateBuilder(args)
               .ConfigureServices(Log.Logger)
               .Build()
               .ConfigureMiddleware();

            await app.RunAsync();
            return 0;
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not run web app");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}