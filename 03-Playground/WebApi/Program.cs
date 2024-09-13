using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using WebApi.CompositionRoot;
using WebApi.DatabaseAccess;

namespace WebApi;

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

            await app.MigrateDatabaseAsync();
            await app.SeedDatabaseAsync();
            await app.RunAsync();
            return 0;
        }
        catch (HostAbortedException e)
        {
            Log.Information(e, "The host was aborted, likely by dotnet ef");
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